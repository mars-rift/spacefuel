Public Class Form1
    ' Game objects
    Private ship As Rectangle
    Private bullets As New List(Of Rectangle)
    Private asteroids As New List(Of Rectangle)
    Private enemies As New List(Of Rectangle)
    Private enemyLasers As New List(Of Rectangle)
    Private random As New Random()

    ' Game settings
    Private ReadOnly shipSpeed As Integer = 5
    Private ReadOnly bulletSpeed As Integer = 8
    Private ReadOnly asteroidSpeed As Integer = 3
    Private ReadOnly enemySpeed As Integer = 2
    Private ReadOnly enemyLaserSpeed As Integer = 6
    Private ReadOnly asteroidSpawnRate As Integer = 50
    Private ReadOnly enemySpawnRate As Integer = 150
    Private ReadOnly enemyFireRate As Integer = 100
    Private score As Integer = 0
    Private isGameOver As Boolean = False

    ' Graphics resources
    Private ReadOnly shipBrush As New SolidBrush(Color.White)
    Private ReadOnly bulletBrush As New SolidBrush(Color.Red)
    Private ReadOnly asteroidBrush As New SolidBrush(Color.DarkSlateGray)
    Private ReadOnly enemyBrush As New SolidBrush(Color.Aqua)
    Private ReadOnly enemyLaserBrush As New SolidBrush(Color.LimeGreen)
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
        enemies.Clear()
        enemyLasers.Clear()

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
            ManageEnemies()
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

            ' Move enemies (side to side and down)
            For i As Integer = enemies.Count - 1 To 0 Step -1
                Dim enemy = enemies(i)

                ' Move horizontally with simple AI to follow player
                If enemy.X + (enemy.Width \ 2) < ship.X + (ship.Width \ 2) Then
                    enemy.X += enemySpeed
                ElseIf enemy.X + (enemy.Width \ 2) > ship.X + (ship.Width \ 2) Then
                    enemy.X -= enemySpeed
                End If

                ' Move down slowly
                If random.Next(30) = 0 Then
                    enemy.Y += 5
                End If

                ' Remove enemies that are off screen
                If enemy.Y > Me.ClientSize.Height Then
                    enemies.RemoveAt(i)
                End If
            Next

            ' Move enemy lasers down
            For i As Integer = enemyLasers.Count - 1 To 0 Step -1
                Dim laser = enemyLasers(i)
                enemyLasers(i) = New Rectangle(laser.X, laser.Y + enemyLaserSpeed, laser.Width, laser.Height)

                ' Remove lasers that are off screen
                If laser.Y > Me.ClientSize.Height Then
                    enemyLasers.RemoveAt(i)
                End If
            Next
        Catch ex As Exception
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

    Private Sub ManageEnemies()
        Try
            ' Spawn new enemies
            If random.Next(enemySpawnRate) = 0 AndAlso enemies.Count < 3 Then
                Dim x As Integer = random.Next(50, Me.ClientSize.Width - 90)
                enemies.Add(New Rectangle(x, 50, 50, 50))
            End If

            ' Enemy firing logic
            For Each enemy In enemies
                If random.Next(enemyFireRate) = 0 Then
                    FireEnemyLaser(enemy)
                End If
            Next
        Catch ex As Exception
            Debug.WriteLine($"Error in ManageEnemies: {ex.Message}")
        End Try
    End Sub

    Private Sub FireEnemyLaser(enemy As Rectangle)
        ' Calculate direction to player for proper firing position
        Dim enemyCenterX As Integer = enemy.X + enemy.Width \ 2
        Dim enemyCenterY As Integer = enemy.Y + enemy.Height \ 2
        Dim directionX As Integer = (ship.X + ship.Width \ 2) - enemyCenterX
        Dim directionY As Integer = (ship.Y + ship.Height \ 2) - enemyCenterY

        ' Normalize and scale direction
        Dim length As Double = Math.Sqrt(directionX * directionX + directionY * directionY)
        If length > 0 Then
            directionX = CInt(directionX / length * enemy.Width / 2)
            directionY = CInt(directionY / length * enemy.Height / 2)
        End If

        ' Create a laser at the front point of the enemy triangle
        enemyLasers.Add(New Rectangle(
            enemyCenterX + directionX - 2,
            enemyCenterY + directionY,
            4, 15))
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

            ' Check bullet-enemy collisions
            For i As Integer = bullets.Count - 1 To 0 Step -1
                For j As Integer = enemies.Count - 1 To 0 Step -1
                    If i < bullets.Count AndAlso j < enemies.Count Then
                        If bullets(i).IntersectsWith(enemies(j)) Then
                            bullets.RemoveAt(i)
                            enemies.RemoveAt(j)
                            score += 300  ' More points for destroying enemies
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

            ' Check ship-enemy collisions
            For Each enemy In enemies
                If ship.IntersectsWith(enemy) Then
                    GameOver()
                    Exit For
                End If
            Next

            ' Check ship-enemy laser collisions
            For i As Integer = enemyLasers.Count - 1 To 0 Step -1
                If enemyLasers(i).IntersectsWith(ship) Then
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

                ' Draw enemies (triangle shape pointing at player)
                For Each enemy In enemies
                    ' Calculate triangle points to point toward player
                    Dim enemyCenterX As Integer = enemy.X + enemy.Width \ 2
                    Dim enemyCenterY As Integer = enemy.Y + enemy.Height \ 2

                    ' Calculate direction to player
                    Dim directionX As Integer = (ship.X + ship.Width \ 2) - enemyCenterX
                    Dim directionY As Integer = (ship.Y + ship.Height \ 2) - enemyCenterY

                    ' Normalize and scale direction
                    Dim length As Double = Math.Sqrt(directionX * directionX + directionY * directionY)
                    If length > 0 Then
                        directionX = CInt(directionX / length * enemy.Width / 2)
                        directionY = CInt(directionY / length * enemy.Height / 2)
                    End If

                    ' Create triangle points (point at player)

                    ' Draw enemy triangle
                    e.Graphics.FillPolygon(enemyBrush, {
                        New Point(enemyCenterX + directionX, enemyCenterY + directionY),                   ' Front point (toward player)
                        New Point(enemyCenterX - directionY - directionX \ 2, enemyCenterY + directionX - directionY \ 2), ' Wing 1
                        New Point(enemyCenterX + directionY - directionX \ 2, enemyCenterY - directionX - directionY \ 2)  ' Wing 2
                    })

                    ' Draw cockpit (circle within triangle)
                    Dim cockpitSize As Integer = enemy.Width \ 3
                    Dim cockpitX As Integer = enemyCenterX - cockpitSize \ 2
                    Dim cockpitY As Integer = enemyCenterY - cockpitSize \ 2
                    e.Graphics.FillEllipse(New SolidBrush(Color.DarkSlateGray), cockpitX, cockpitY, cockpitSize, cockpitSize)
                Next

                ' Draw enemy lasers
                For Each laser In enemyLasers
                    e.Graphics.FillRectangle(enemyLaserBrush, laser)
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
            enemyBrush.Dispose()
            enemyLaserBrush.Dispose()
            scoreBrush.Dispose()
            scoreFont.Dispose()
            gameOverFont.Dispose()
            gameTimer.Dispose()
        Catch ex As Exception
            Debug.WriteLine($"Error in OnClosing: {ex.Message}")
        End Try
    End Sub
End Class
