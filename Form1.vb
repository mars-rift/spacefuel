Public Class Form1
    ' Game objects
    Private ship As Rectangle
    Private bullets As New List(Of Rectangle)
    Private asteroids As New List(Of Rectangle)
    Private random As New Random()

    ' Game settings
    Private ReadOnly shipSpeed As Integer = 5
    Private ReadOnly bulletSpeed As Integer = 8
    Private ReadOnly asteroidSpeed As Integer = 3
    Private ReadOnly asteroidSpawnRate As Integer = 50
    Private score As Integer = 0
    Private isGameOver As Boolean = False

    ' Graphics resources
    Private ReadOnly shipBrush As New SolidBrush(Color.White)
    Private ReadOnly bulletBrush As New SolidBrush(Color.Red)
    Private ReadOnly asteroidBrush As New SolidBrush(Color.DarkSlateGray)
    Private ReadOnly scoreBrush As New SolidBrush(Color.White)
    Private ReadOnly scoreFont As New Font("Arial", 20, FontStyle.Bold)
    Private ReadOnly gameOverFont As New Font("Arial", 48, FontStyle.Bold)

    ' Game timer
    Private WithEvents gameTimer As New Timer()

    Public Sub New()
        InitializeComponent()
        InitializeGame()
    End Sub

    Private Sub InitializeGame()
        ' Set form properties
        Me.DoubleBuffered = True
        Me.BackColor = Color.Black
        Me.WindowState = FormWindowState.Normal
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.ClientSize = New Size(800, 600)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.KeyPreview = True

        ' Reset game state
        score = 0
        isGameOver = False
        bullets.Clear()
        asteroids.Clear()

        ' Initialize ship in the center bottom of the screen
        ship = New Rectangle(Me.ClientSize.Width \ 2 - 20, Me.ClientSize.Height - 50, 40, 40)

        ' Set up game timer
        gameTimer.Interval = 16 ' Approximately 60 FPS
        gameTimer.Enabled = True
    End Sub

    Private Sub GameLoop() Handles gameTimer.Tick
        If Not isGameOver Then
            MoveGameObjects()
            CheckCollisions()
            CreateAsteroids()
            Me.Invalidate() ' Trigger repaint
        End If
    End Sub

    Private Sub MoveGameObjects()
        Try
            ' Move bullets up
            For i As Integer = bullets.Count - 1 To 0 Step -1
                Dim bullet = bullets(i)
                bullets(i) = New Rectangle(bullet.X, bullet.Y - bulletSpeed, bullet.Width, bullet.Height)

                ' Remove bullets that are off screen
                If bullet.Y < 0 Then
                    bullets.RemoveAt(i)
                End If
            Next

            ' Move asteroids down
            For i As Integer = asteroids.Count - 1 To 0 Step -1
                Dim asteroid = asteroids(i)
                asteroids(i) = New Rectangle(asteroid.X, asteroid.Y + asteroidSpeed, asteroid.Width, asteroid.Height)

                ' Remove asteroids that are off screen
                If asteroid.Y > Me.ClientSize.Height Then
                    asteroids.RemoveAt(i)
                End If
            Next
        Catch ex As Exception
            ' Handle any potential errors during movement
            Debug.WriteLine($"Error in MoveGameObjects: {ex.Message}")
        End Try
    End Sub

    Private Sub CreateAsteroids()
        Try
            If random.Next(asteroidSpawnRate) = 0 Then
                Dim x As Integer = random.Next(0, Me.ClientSize.Width - 40)
                asteroids.Add(New Rectangle(x, -50, 40, 40))
            End If
        Catch ex As Exception
            Debug.WriteLine($"Error in CreateAsteroids: {ex.Message}")
        End Try
    End Sub

    Private Sub CheckCollisions()
        Try
            ' Check bullet-asteroid collisions
            For i As Integer = bullets.Count - 1 To 0 Step -1
                For j As Integer = asteroids.Count - 1 To 0 Step -1
                    If i < bullets.Count AndAlso j < asteroids.Count Then
                        If bullets(i).IntersectsWith(asteroids(j)) Then
                            bullets.RemoveAt(i)
                            asteroids.RemoveAt(j)
                            score += 100
                            Exit For
                        End If
                    End If
                Next
            Next

            ' Check ship-asteroid collisions
            For Each asteroid In asteroids
                If ship.IntersectsWith(asteroid) Then
                    GameOver()
                    Exit For
                End If
            Next
        Catch ex As Exception
            Debug.WriteLine($"Error in CheckCollisions: {ex.Message}")
        End Try
    End Sub

    Private Sub GameOver()
        isGameOver = True
        gameTimer.Enabled = False
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Try
            MyBase.OnPaint(e)

            ' Draw using anti-aliasing
            e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

            ' Draw game objects
            If Not isGameOver Then
                ' Draw ship
                Dim points() As Point = {
                    New Point(ship.X + ship.Width \ 2, ship.Y),
                    New Point(ship.X, ship.Y + ship.Height),
                    New Point(ship.X + ship.Width, ship.Y + ship.Height)
                }
                e.Graphics.FillPolygon(shipBrush, points)

                ' Draw bullets
                For Each bullet In bullets
                    e.Graphics.FillEllipse(bulletBrush, bullet)
                Next

                ' Draw asteroids
                For Each asteroid In asteroids
                    e.Graphics.FillEllipse(asteroidBrush, asteroid)
                Next
            End If

            ' Draw score
            e.Graphics.DrawString($"Score: {score}", scoreFont, scoreBrush, 20, 20)

            ' Draw game over message
            If isGameOver Then
                Dim gameOverText As String = "GAME OVER!"
                Dim textSize = e.Graphics.MeasureString(gameOverText, gameOverFont)
                Dim x As Single = (Me.ClientSize.Width - textSize.Width) / 2
                Dim y As Single = (Me.ClientSize.Height - textSize.Height) / 2
                e.Graphics.DrawString(gameOverText, gameOverFont, scoreBrush, x, y)

                ' Draw restart and quit instructions
                Dim instructionText As String = "Press R to Restart or Q to Quit"
                Dim instructionSize = e.Graphics.MeasureString(instructionText, scoreFont)
                x = (Me.ClientSize.Width - instructionSize.Width) / 2
                y += textSize.Height + 20
                e.Graphics.DrawString(instructionText, scoreFont, scoreBrush, x, y)
            End If



        Catch ex As Exception
            Debug.WriteLine($"Error in OnPaint: {ex.Message}")
        End Try
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        Try
            MyBase.OnKeyDown(e)

            If isGameOver Then
                Select Case e.KeyCode
                    Case Keys.R
                        InitializeGame()
                    Case Keys.Q
                        Me.Close()  ' This will trigger the OnClosing event and clean up resources
                End Select
                Return
            End If

            ' Ship movement
            Select Case e.KeyCode
                Case Keys.Left
                    If ship.X > 0 Then
                        ship.X -= shipSpeed
                    End If
                Case Keys.Right
                    If ship.X < Me.ClientSize.Width - ship.Width Then
                        ship.X += shipSpeed
                    End If
                Case Keys.Space
                    ' Shoot bullet
                    bullets.Add(New Rectangle(
                    ship.X + ship.Width \ 2 - 2,
                    ship.Y,
                    4, 10))
            End Select

        Catch ex As Exception
            Debug.WriteLine($"Error in OnKeyDown: {ex.Message}")
        End Try
    End Sub


    Protected Overrides Sub OnClosing(e As System.ComponentModel.CancelEventArgs)
        Try
            MyBase.OnClosing(e)
            ' Clean up resources
            shipBrush.Dispose()
            bulletBrush.Dispose()
            asteroidBrush.Dispose()
            scoreBrush.Dispose()
            scoreFont.Dispose()
            gameOverFont.Dispose()
            gameTimer.Dispose()
        Catch ex As Exception
            Debug.WriteLine($"Error in OnClosing: {ex.Message}")
        End Try
    End Sub
End Class
