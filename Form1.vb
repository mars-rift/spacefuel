Public Class Form1
    ' Game objects
    Private ship As Rectangle
    Private bullets As New List(Of Rectangle)
    Private asteroids As New List(Of Rectangle)
    Private asteroidDetails As New List(Of Point()) ' Store crater and highlight positions
    Private enemies As New List(Of Rectangle)
    Private enemyLasers As New List(Of Rectangle)
    Private powerUps As New List(Of Rectangle)
    Private powerUpTypes As New List(Of Integer) ' 0=Shield, 1=DoubleShot, 2=ExtraLife
    Private ReadOnly powerUpBrushes As SolidBrush() = {
        New SolidBrush(Color.Blue),     ' Shield
        New SolidBrush(Color.Yellow),   ' DoubleShot
        New SolidBrush(Color.OrangeRed)      ' ExtraLife
    }
    Private hasPowerUp As Boolean = False
    Private powerUpTimer As Integer = 0
    Private powerUpType As Integer = -1
    Private ReadOnly powerUpSpawnRate As Integer = 700
    Private random As New Random()
    Private explosions As New List(Of Rectangle)
    Private explosionTimes As New List(Of Integer)
    Private ReadOnly explosionMaxTime As Integer = 20
    Private nearMisses As New List(Of Rectangle)
    Private nearMissTimers As New List(Of Integer)
    Private ReadOnly nearMissMaxTime As Integer = 10

    ' Star field for background
    Private stars As New List(Of Point)
    Private starSpeeds As New List(Of Integer)
    Private ReadOnly maxStars As Integer = 100

    ' Game settings
    Private ReadOnly shipSpeed As Integer = 5
    Private ReadOnly bulletSpeed As Integer = 8
    Private asteroidSpeed As Integer = 3
    Private enemySpeed As Integer = 4 ' Increased from 2 to 4
    Private ReadOnly enemyLaserSpeed As Integer = 8 ' Increased from 6 to 8
    Private asteroidSpawnRate As Integer = 50
    Private enemySpawnRate As Integer = 150
    Private ReadOnly enemyFireRate As Integer = 60 ' Reduced from 100 to 60 (fires more often)
    Private score As Integer = 0
    Private isGameOver As Boolean = False
    Private lives As Integer = 3
    Private ReadOnly lifeIconBrush As New SolidBrush(Color.Red)
    Private playerInvulnerable As Boolean = False
    Private invulnerabilityTimer As Integer = 0
    Private shieldActive As Boolean = False

    ' Power-up constants
    Private Const POWERUP_DOUBLESHOOT As Integer = 1
    Private Const POWERUP_SHIELD As Integer = 0
    Private Const POWERUP_EXTRALIFE As Integer = 2

    ' Power-up rarity settings (percentages)
    Private Const DOUBLESHOOT_CHANCE As Integer = 60
    Private Const SHIELD_CHANCE As Integer = 30
    Private Const EXTRALIFE_CHANCE As Integer = 10  ' This adds up to 100%

    ' Level settings
    Private level As Integer = 1
    Private enemiesDefeated As Integer = 0
    Private enemiesForNextLevel As Integer = 10

    ' Combo settings
    Private combo As Integer = 0
    Private comboTimer As Integer = 0
    Private ReadOnly comboMaxTime As Integer = 120 ' 2 seconds at 60fps

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
    Private WithEvents GameTimer As New Timer()

    ' Input state
    Private leftPressed As Boolean = False
    Private rightPressed As Boolean = False
    Private spacePressed As Boolean = False
    Private fireDelay As Integer = 0

    Private isPaused As Boolean = False

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
        
        ' Performance optimizations
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.DoubleBuffer, True)
        Me.UpdateStyles()

        ' Reset game state
        score = 0
        isGameOver = False
        bullets.Clear()
        asteroids.Clear()
        asteroidDetails.Clear()
        enemies.Clear()
        enemyLasers.Clear()
        powerUps.Clear()
        powerUpTypes.Clear()
        explosions.Clear()
        explosionTimes.Clear()
        nearMisses.Clear()
        nearMissTimers.Clear()
        lives = 3
        playerInvulnerable = False
        invulnerabilityTimer = 0
        shieldActive = False
        hasPowerUp = False
        powerUpTimer = 0
        powerUpType = -1
        level = 1
        enemiesDefeated = 0
        enemiesForNextLevel = 10
        combo = 0
        comboTimer = 0
        leftPressed = False
        rightPressed = False
        spacePressed = False
        fireDelay = 0
        isPaused = False

        ' Reset difficulty settings
        asteroidSpeed = 3
        enemySpeed = 4
        asteroidSpawnRate = 50
        enemySpawnRate = 150

        ' Initialize ship in the center bottom of the screen
        ship = New Rectangle(Me.ClientSize.Width \ 2 - 20, Me.ClientSize.Height - 50, 40, 40)

        ' Initialize star field
        InitializeStarField()

        ' Set up game timer
        GameTimer.Interval = 16 ' Approximately 60 FPS
        GameTimer.Enabled = True
    End Sub

    Private Sub InitializeStarField()
        stars.Clear()
        starSpeeds.Clear()
        
        For i As Integer = 0 To maxStars - 1
            stars.Add(New Point(random.Next(Me.ClientSize.Width), random.Next(Me.ClientSize.Height)))
            starSpeeds.Add(random.Next(1, 4)) ' Different speeds for parallax effect
        Next
    End Sub

    Private Sub UpdateStarField()
        For i As Integer = 0 To stars.Count - 1
            Dim star As Point = stars(i)
            star.Y += starSpeeds(i)
            
            ' Reset star to top if it goes off screen
            If star.Y > Me.ClientSize.Height Then
                star.Y = -5
                star.X = random.Next(Me.ClientSize.Width)
            End If
            
            stars(i) = star
        Next
    End Sub

    Private Sub GameLoop() Handles GameTimer.Tick
        If Not isGameOver AndAlso Not isPaused Then
            UpdateStarField()
            HandleInput()
            MoveGameObjects()
            CheckCollisions()
            CreateAsteroids()
            ManageEnemies()
            CreatePowerUps()
            CheckPowerUpCollisions()
            ManagePowerUps() ' Keep only one instance of this call
            HandleInvulnerability()
            UpdateExplosions()
            UpdateCombo()
            UpdateNearMisses()
            CheckLevelProgression()
            Me.Invalidate() ' Trigger repaint
        End If
    End Sub

    Private Sub HandleInput()
        ' Ship movement
        If leftPressed AndAlso ship.X > 0 Then
            ship.X -= shipSpeed
        End If
        If rightPressed AndAlso ship.X < Me.ClientSize.Width - ship.Width Then
            ship.X += shipSpeed
        End If

        ' Shooting with fire rate limit
        If spacePressed AndAlso fireDelay <= 0 Then
            ' Normal shot (centered)
            bullets.Add(New Rectangle(
                ship.X + ship.Width \ 2 - 2,
                ship.Y,
                4, 10))

            ' Double shot if power-up active (add spread)
            If hasPowerUp AndAlso powerUpType = POWERUP_DOUBLESHOOT Then
                ' Add a second bullet (offset to the side)
                bullets.Add(New Rectangle(
                    ship.X + ship.Width \ 2 - 8, ' Offset to the left
                    ship.Y,
                    4, 10))

                ' Add a third bullet (offset to the other side)
                bullets.Add(New Rectangle(
                    ship.X + ship.Width \ 2 + 4, ' Offset to the right
                    ship.Y,
                    4, 10))
            End If

            fireDelay = 10 ' Adjust for desired fire rate
        End If

        If fireDelay > 0 Then
            fireDelay -= 1
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
                    asteroidDetails.RemoveAt(i) ' Remove corresponding details
                End If
            Next

            ' Move enemies (side to side and down)
            For i As Integer = enemies.Count - 1 To 0 Step -1
                Dim enemy = enemies(i)

                ' Improved AI movement - more aggressive pursuit
                Dim playerCenterX As Integer = ship.X + ship.Width \ 2
                Dim enemyCenterX As Integer = enemy.X + enemy.Width \ 2
                Dim distance As Integer = Math.Abs(playerCenterX - enemyCenterX)

                ' Move horizontally with enhanced AI
                If enemyCenterX < playerCenterX Then
                    enemy.X += enemySpeed
                ElseIf enemyCenterX > playerCenterX Then
                    enemy.X -= enemySpeed
                End If

                ' More aggressive downward movement
                If random.Next(20) = 0 Then ' Increased from 30 to 20 for more frequent movement
                    enemy.Y += 8 ' Increased from 5 to 8
                End If

                ' Additional side-to-side movement for evasion
                If random.Next(40) = 0 Then
                    enemy.X += If(random.Next(2) = 0, -15, 15)
                    ' Keep enemy within screen bounds
                    If enemy.X < 0 Then enemy.X = 0
                    If enemy.X > Me.ClientSize.Width - enemy.Width Then enemy.X = Me.ClientSize.Width - enemy.Width
                End If

                enemies(i) = enemy

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
                
                ' Pre-calculate visual details for consistent rendering
                Dim details(2) As Point
                details(0) = New Point(random.Next(20), random.Next(20)) ' Crater offset
                details(1) = New Point(random.Next(15), random.Next(15)) ' Highlight offset
                details(2) = New Point(random.Next(10, 20), random.Next(5, 15)) ' Sizes
                asteroidDetails.Add(details)
            End If
        Catch ex As Exception
            Debug.WriteLine($"Error in CreateAsteroids: {ex.Message}")
        End Try
    End Sub

    Private Sub ManageEnemies()
        Try
            ' Spawn new enemies - increased max count and improved spawn rate
            If random.Next(enemySpawnRate) = 0 AndAlso enemies.Count < 5 Then ' Increased from 3 to 5
                Dim x As Integer = random.Next(50, Me.ClientSize.Width - 90)
                enemies.Add(New Rectangle(x, 50, 60, 60)) ' Increased size from 50x50 to 60x60
            End If

            ' Enemy firing logic - more aggressive firing patterns
            For Each enemy In enemies
                ' Predictive firing - aim where player will be
                If random.Next(enemyFireRate) = 0 Then
                    FirePredictiveLaser(enemy)
                End If
                
                ' Occasional burst fire
                If random.Next(200) = 0 Then
                    For burst As Integer = 0 To 2
                        FireEnemyLaser(enemy)
                    Next
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

    Private Sub FirePredictiveLaser(enemy As Rectangle)
        ' Predictive firing - aim where player will be based on movement
        Dim enemyCenterX As Integer = enemy.X + enemy.Width \ 2
        Dim enemyCenterY As Integer = enemy.Y + enemy.Height \ 2
        
        ' Predict player movement
        Dim predictedPlayerX As Integer = ship.X + ship.Width \ 2
        If leftPressed Then
            predictedPlayerX -= shipSpeed * 10 ' Predict 10 frames ahead
        ElseIf rightPressed Then
            predictedPlayerX += shipSpeed * 10
        End If
        
        Dim directionX As Integer = predictedPlayerX - enemyCenterX
        Dim directionY As Integer = (ship.Y + ship.Height \ 2) - enemyCenterY

        ' Normalize and scale direction
        Dim length As Double = Math.Sqrt(directionX * directionX + directionY * directionY)
        If length > 0 Then
            directionX = CInt(directionX / length * enemy.Width / 2)
            directionY = CInt(directionY / length * enemy.Height / 2)
        End If

        ' Create a predictive laser
        enemyLasers.Add(New Rectangle(
            enemyCenterX + directionX - 2,
            enemyCenterY + directionY,
            5, 18)) ' Slightly larger and longer laser
    End Sub

    Private Sub CreatePowerUps()
        If random.Next(powerUpSpawnRate) = 0 Then
            Dim x As Integer = random.Next(0, Me.ClientSize.Width - 30)
            powerUps.Add(New Rectangle(x, -30, 30, 30))

            ' Use weighted probability instead of random.Next(3)
            Dim chance As Integer = random.Next(100) ' Generate number between 0-99

            Dim powerUpType As Integer
            If chance < DOUBLESHOOT_CHANCE Then
                powerUpType = POWERUP_DOUBLESHOOT    ' Most common
            ElseIf chance < DOUBLESHOOT_CHANCE + SHIELD_CHANCE Then
                powerUpType = POWERUP_SHIELD         ' Less common
            Else
                powerUpType = POWERUP_EXTRALIFE      ' Rare
            End If

            powerUpTypes.Add(powerUpType)
        End If

        ' Move power-ups
        For i As Integer = powerUps.Count - 1 To 0 Step -1
            Dim powerUp = powerUps(i)
            powerUps(i) = New Rectangle(powerUp.X, powerUp.Y + 2, powerUp.Width, powerUp.Height)

            ' Remove if off-screen
            If powerUp.Y > Me.ClientSize.Height Then
                powerUps.RemoveAt(i)
                powerUpTypes.RemoveAt(i)
            End If
        Next
    End Sub

    Private Sub CheckCollisions()
        Try
            ' Check bullet-asteroid collisions
            For i As Integer = bullets.Count - 1 To 0 Step -1
                For j As Integer = asteroids.Count - 1 To 0 Step -1
                    If i < bullets.Count AndAlso j < asteroids.Count Then
                        ' Use expanded collision area for more forgiving hits
                        Dim bulletArea = New Rectangle(
                            bullets(i).X - 2,
                            bullets(i).Y - 2,
                            bullets(i).Width + 4,
                            bullets(i).Height + 4)

                        If bulletArea.IntersectsWith(asteroids(j)) Then
                            CreateExplosion(asteroids(j).X + asteroids(j).Width \ 2, asteroids(j).Y + asteroids(j).Height \ 2, 40)
                            bullets.RemoveAt(i)
                            asteroids.RemoveAt(j)
                            asteroidDetails.RemoveAt(j) ' Remove corresponding details
                            AddScore(100)
                            ' Console.Beep(800, 100) ' Removed for better performance
                            Exit For
                        End If
                    End If
                Next
            Next

            ' Check bullet-enemy collisions
            For i As Integer = bullets.Count - 1 To 0 Step -1
                For j As Integer = enemies.Count - 1 To 0 Step -1
                    If i < bullets.Count AndAlso j < enemies.Count Then
                        ' Use expanded collision area for more forgiving hits
                        Dim bulletArea = New Rectangle(
                            bullets(i).X - 2,
                            bullets(i).Y - 2,
                            bullets(i).Width + 4,
                            bullets(i).Height + 4)

                        If bulletArea.IntersectsWith(enemies(j)) Then
                            CreateExplosion(enemies(j).X + enemies(j).Width \ 2, enemies(j).Y + enemies(j).Height \ 2, 50)
                            bullets.RemoveAt(i)
                            enemies.RemoveAt(j)
                            AddScore(300)  ' More points for destroying enemies
                            enemiesDefeated += 1
                            ' Console.Beep(600, 150) ' Removed for better performance
                            Exit For
                        End If
                    End If
                Next
            Next

            ' Check ship-asteroid collisions with more forgiving hitbox
            For Each asteroid In asteroids
                ' Create a slightly smaller hitbox for player ship
                Dim shipHitbox = New Rectangle(
                    ship.X + 5,  ' Shrink from left
                    ship.Y + 10, ' Shrink from top
                    ship.Width - 10, ' Shrink width
                    ship.Height - 10) ' Shrink height

                If shipHitbox.IntersectsWith(asteroid) AndAlso Not playerInvulnerable AndAlso Not (shieldActive AndAlso hasPowerUp AndAlso powerUpType = POWERUP_SHIELD) Then
                    GameOver()
                    Exit For
                End If
            Next

            ' Apply the same hitbox changes to enemy and laser collisions
            Dim playerHitbox = New Rectangle(
                ship.X + 5,
                ship.Y + 10,
                ship.Width - 10,
                ship.Height - 10)

            ' Check ship-enemy collisions
            For Each enemy In enemies
                If playerHitbox.IntersectsWith(enemy) AndAlso Not playerInvulnerable AndAlso Not (shieldActive AndAlso hasPowerUp AndAlso powerUpType = POWERUP_SHIELD) Then
                    GameOver()
                    Exit For
                End If
            Next

            ' Check ship-enemy laser collisions
            For i As Integer = enemyLasers.Count - 1 To 0 Step -1
                If i < enemyLasers.Count AndAlso enemyLasers(i).IntersectsWith(playerHitbox) AndAlso Not (shieldActive AndAlso hasPowerUp AndAlso powerUpType = POWERUP_SHIELD) Then
                    ' Create small explosion at hit point for visual feedback
                    CreateExplosion(ship.X + ship.Width \ 2, ship.Y + ship.Height \ 2, 30)
                    ' Remove the laser that hit the player
                    enemyLasers.RemoveAt(i)
                    GameOver()
                    Exit For
                End If
            Next
        Catch ex As Exception
            Debug.WriteLine($"Error in CheckCollisions: {ex.Message}")
        End Try
    End Sub

    Private Sub CheckPowerUpCollisions()
        Try
            ' Check ship-powerup collisions
            For i As Integer = powerUps.Count - 1 To 0 Step -1
                If ship.IntersectsWith(powerUps(i)) Then
                    ' Apply power-up effect
                    ApplyPowerUp(powerUpTypes(i))

                    ' Debug line to verify collection
                    Debug.WriteLine($"Collected power-up type: {powerUpTypes(i)}")
                    ' Console.Beep(1000, 200) ' Removed for better performance

                    ' Remove collected power-up
                    powerUps.RemoveAt(i)
                    powerUpTypes.RemoveAt(i)
                End If
            Next
        Catch ex As Exception
            Debug.WriteLine($"Error in CheckPowerUpCollisions: {ex.Message}")
        End Try
    End Sub

    Private Sub ApplyPowerUp(type As Integer)
        ' Deactivate current power-up if any
        If hasPowerUp Then
            ' Reset any power-up specific effects
            Select Case powerUpType
                Case POWERUP_SHIELD ' Shield - deactivate shield
                    shieldActive = False
                Case POWERUP_DOUBLESHOOT ' DoubleShot - nothing specific to reset
                Case POWERUP_EXTRALIFE ' ExtraLife - already applied
            End Select
        End If

        ' Apply new power-up
        hasPowerUp = True
        powerUpType = type
        powerUpTimer = 600 ' 10 seconds at 60fps

        ' Power-up specific effects
        Select Case type
            Case POWERUP_SHIELD ' Shield
                shieldActive = True

            Case POWERUP_DOUBLESHOOT ' DoubleShot
                ' Will be handled in HandleInput shooting logic

            Case POWERUP_EXTRALIFE ' ExtraLife
                lives += 1
                hasPowerUp = False ' Immediate effect, no duration
                powerUpType = -1
                shieldActive = False ' Make sure shield is off for ExtraLife
        End Select
    End Sub

    Private Sub ManagePowerUps()
        If hasPowerUp AndAlso powerUpType >= 0 Then
            powerUpTimer -= 1

            ' Power-up expired
            If powerUpTimer <= 0 Then
                ' Handle power-up expiration
                Select Case powerUpType
                    Case POWERUP_SHIELD ' Shield
                        shieldActive = False
                    Case POWERUP_DOUBLESHOOT ' DoubleShot
                        ' No specific cleanup needed
                End Select

                ' Reset power-up state
                hasPowerUp = False
                powerUpType = -1
            End If
        End If
    End Sub

    Private Sub CheckLevelProgression()
        If enemiesDefeated >= enemiesForNextLevel Then
            level += 1
            enemiesDefeated = 0
            enemiesForNextLevel += 5 ' Increase enemies needed for next level

            ' Increase difficulty more aggressively
            If asteroidSpawnRate > 10 Then asteroidSpawnRate -= 5
            If enemySpawnRate > 20 Then enemySpawnRate -= 15 ' Faster enemy spawns
            If enemySpeed < 8 Then enemySpeed += 1 ' Enemies get faster each level
            
            ' Show level up notification
            Debug.WriteLine($"Level {level}! Enemy speed: {enemySpeed}, Enemy spawn rate: {enemySpawnRate}")
        End If
    End Sub

    Private Sub GameOver()
        lives -= 1
        If lives <= 0 Then
            isGameOver = True
            GameTimer.Enabled = False
        Else
            ' Reset player position but continue game
            ship = New Rectangle(Me.ClientSize.Width \ 2 - 20, Me.ClientSize.Height - 50, 40, 40)
            ' Add brief invulnerability time
            playerInvulnerable = True
            invulnerabilityTimer = 120 ' 2 seconds at 60fps
        End If
    End Sub

    Private Sub HandleInvulnerability()
        If playerInvulnerable Then
            invulnerabilityTimer -= 1
            If invulnerabilityTimer <= 0 Then
                playerInvulnerable = False
            End If
        End If
    End Sub

    Private Sub CreateExplosion(x As Integer, y As Integer, size As Integer)
        explosions.Add(New Rectangle(x - size \ 2, y - size \ 2, size, size))
        explosionTimes.Add(explosionMaxTime)
    End Sub

    Private Sub UpdateExplosions()
        For i As Integer = explosionTimes.Count - 1 To 0 Step -1
            explosionTimes(i) -= 1
            If explosionTimes(i) <= 0 Then
                explosions.RemoveAt(i)
                explosionTimes.RemoveAt(i)
            End If
        Next
    End Sub

    Private Sub CreateNearMiss(x As Integer, y As Integer)
        nearMisses.Add(New Rectangle(x - 5, y - 5, 10, 10))
        nearMissTimers.Add(nearMissMaxTime)
    End Sub

    Private Sub UpdateNearMisses()
        For i As Integer = nearMissTimers.Count - 1 To 0 Step -1
            nearMissTimers(i) -= 1
            If nearMissTimers(i) <= 0 Then
                nearMisses.RemoveAt(i)
                nearMissTimers.RemoveAt(i)
            End If
        Next
    End Sub

    Private Sub AddScore(basePoints As Integer)
        combo += 1
        comboTimer = comboMaxTime

        ' Calculate score with multiplier
        Dim multiplier As Double = 1.0 + (combo - 1) * 0.1 ' 10% increase per combo
        score += CInt(basePoints * multiplier)
    End Sub

    Private Sub UpdateCombo()
        If combo > 0 Then
            comboTimer -= 1
            If comboTimer <= 0 Then
                combo = 0
            End If
        End If
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Try
            MyBase.OnPaint(e)

            ' Draw using anti-aliasing
            e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

            ' Draw star field background (optimized)
            For i As Integer = 0 To stars.Count - 1
                Dim brightness As Integer = 100 + (starSpeeds(i) * 50) ' Faster stars are brighter
                Dim starSize As Integer = If(starSpeeds(i) > 2, 2, 1) ' Faster stars are bigger
                Dim starColor As Color = Color.FromArgb(brightness, brightness, brightness)
                e.Graphics.FillRectangle(New SolidBrush(starColor), stars(i).X, stars(i).Y, starSize, starSize)
            Next

            ' Draw game objects
            If Not isGameOver Then
                ' Draw ship (with invulnerability flashing effect)
                Dim points() As Point = {
                    New Point(ship.X + ship.Width \ 2, ship.Y),
                    New Point(ship.X, ship.Y + ship.Height),
                    New Point(ship.X + ship.Width, ship.Y + ship.Height)
                }
                
                ' Flash ship during invulnerability (but always draw if not invulnerable)
                If Not playerInvulnerable OrElse (invulnerabilityTimer Mod 8) < 4 Then
                    e.Graphics.FillPolygon(shipBrush, points)
                End If

                ' Draw shield if active
                If shieldActive AndAlso hasPowerUp AndAlso powerUpType = POWERUP_SHIELD Then
                    Dim shieldRect As New Rectangle(
                        ship.X - 5, ship.Y - 5,
                        ship.Width + 10, ship.Height + 10)
                    e.Graphics.DrawEllipse(New Pen(Color.Blue, 2), shieldRect)
                End If

                ' Draw bullets
                For Each bullet In bullets
                    e.Graphics.FillEllipse(bulletBrush, bullet)
                Next

                ' Draw asteroids with pre-calculated details
                For i As Integer = 0 To asteroids.Count - 1
                    If i < asteroidDetails.Count Then
                        Dim asteroid As Rectangle = asteroids(i)
                        Dim details As Point() = asteroidDetails(i)
                        
                        ' Draw outer asteroid shape
                        e.Graphics.FillEllipse(asteroidBrush, asteroid)

                        ' Draw crater using pre-calculated position
                        Dim craterSize As Integer = details(2).X ' Use pre-calculated size
                        Dim craterX As Integer = asteroid.X + details(0).X
                        Dim craterY As Integer = asteroid.Y + details(0).Y
                        e.Graphics.FillEllipse(New SolidBrush(Color.FromArgb(30, 30, 30)), craterX, craterY, craterSize, craterSize)

                        ' Draw highlight using pre-calculated position
                        Dim highlightSize As Integer = details(2).Y ' Use pre-calculated size
                        Dim highlightX As Integer = asteroid.X + details(1).X
                        Dim highlightY As Integer = asteroid.Y + details(1).Y
                        e.Graphics.FillEllipse(New SolidBrush(Color.FromArgb(80, 80, 80)), highlightX, highlightY, highlightSize, highlightSize)
                    End If
                Next

                ' Draw enemies (triangle shape pointing at player)
                For Each enemy In enemies
                    ' Calculate triangle points to point toward player
                    Dim enemyCenterX As Integer = enemy.X + enemy.Width \ 2
                    Dim enemyCenterY As Integer = enemy.Y + enemy.Height \ 2

                    ' Calculate direction to player
                    Dim directionX As Integer = (ship.X + ship.Width \ 2) - enemyCenterX
                    Dim directionY As Integer = (ship.Y + ship.Height \ 2) - enemyCenterY

                    ' Normalize and scale direction for larger, more visible enemies
                    Dim length As Double = Math.Sqrt(directionX * directionX + directionY * directionY)
                    If length > 0 Then
                        directionX = CInt(directionX / length * 25) ' Increased from 20 to 25
                        directionY = CInt(directionY / length * 25)
                    Else
                        ' Default downward pointing if no direction can be calculated
                        directionX = 0
                        directionY = 25 ' Increased from 20 to 25
                    End If

                    ' Create larger triangle points with improved wing geometry
                    Dim frontX As Integer = enemyCenterX + directionX
                    Dim frontY As Integer = enemyCenterY + directionY
                    
                    ' Calculate perpendicular vector for larger wings
                    Dim perpX As Integer = -directionY
                    Dim perpY As Integer = directionX
                    
                    ' Draw enemy triangle with larger, more visible geometry
                    e.Graphics.FillPolygon(enemyBrush, {
                        New Point(frontX, frontY),                                          ' Front point (toward player)
                        New Point(enemyCenterX - perpX * 3 \ 4, enemyCenterY - perpY * 3 \ 4), ' Left wing (larger)
                        New Point(enemyCenterX + perpX * 3 \ 4, enemyCenterY + perpY * 3 \ 4)  ' Right wing (larger)
                    })

                    ' Draw cockpit (larger circle within triangle)
                    Dim cockpitSize As Integer = enemy.Width \ 3 ' Increased back to 1/3 for visibility
                    Dim cockpitX As Integer = enemyCenterX - cockpitSize \ 2
                    Dim cockpitY As Integer = enemyCenterY - cockpitSize \ 2
                    e.Graphics.FillEllipse(New SolidBrush(Color.DarkSlateGray), cockpitX, cockpitY, cockpitSize, cockpitSize)
                    
                    ' Add engine glow effect for better visibility
                    Dim engineSize As Integer = 8
                    Dim engineX As Integer = enemyCenterX - directionX \ 2 - engineSize \ 2
                    Dim engineY As Integer = enemyCenterY - directionY \ 2 - engineSize \ 2
                    e.Graphics.FillEllipse(New SolidBrush(Color.FromArgb(150, Color.Red)), engineX, engineY, engineSize, engineSize)
                Next

                ' Draw enemy lasers with enhanced visuals
                For Each laser In enemyLasers
                    ' Draw main laser beam
                    e.Graphics.FillRectangle(enemyLaserBrush, laser)
                    ' Add glow effect
                    Dim glowRect As New Rectangle(laser.X - 1, laser.Y - 1, laser.Width + 2, laser.Height + 2)
                    e.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(100, Color.LimeGreen)), glowRect)
                Next

                ' Draw power-ups (add after drawing enemy lasers)
                For i As Integer = 0 To powerUps.Count - 1
                    ' Select brush based on power-up type
                    If i < powerUpTypes.Count Then
                        Dim type As Integer = powerUpTypes(i)
                        If type >= 0 AndAlso type < powerUpBrushes.Length Then
                            e.Graphics.FillEllipse(powerUpBrushes(type), powerUps(i))
                        End If
                    End If
                Next

                ' Draw explosions
                For i As Integer = 0 To explosions.Count - 1
                    If i < explosionTimes.Count Then
                        Dim timeLeft As Integer = explosionTimes(i)
                        Dim maxTime As Integer = explosionMaxTime
                        Dim progress As Single = 1.0F - (timeLeft / maxTime)
                        
                        ' Create expanding explosion effect
                        Dim explosion As Rectangle = explosions(i)
                        Dim expandedSize As Integer = CInt(explosion.Width * (1 + progress * 0.5))
                        Dim expandedExplosion As New Rectangle(
                            explosion.X - (expandedSize - explosion.Width) \ 2,
                            explosion.Y - (expandedSize - explosion.Height) \ 2,
                            expandedSize, expandedSize)
                            
                        ' Fade from orange to red
                        Dim alpha As Integer = CInt(255 * (1 - progress))
                        Dim explosionColor As Color = Color.FromArgb(alpha, 255, CInt(128 * (1 - progress)), 0)
                        
                        e.Graphics.FillEllipse(New SolidBrush(explosionColor), expandedExplosion)
                    End If
                Next

                ' Draw near misses
                For Each nearMiss In nearMisses
                    e.Graphics.FillEllipse(New SolidBrush(Color.Green), nearMiss)
                Next

                ' Draw near misses (visual feedback)
                For Each nearMiss In nearMisses
                    e.Graphics.DrawEllipse(New Pen(Color.White, 1), nearMiss)
                Next
            End If

            ' Draw score
            e.Graphics.DrawString($"Score: {score}", scoreFont, scoreBrush, 20, 20)

            ' Draw level
            e.Graphics.DrawString($"Level: {level}", scoreFont, scoreBrush, 20, 50)

            ' Draw combo (if active)
            If combo > 1 Then
                e.Graphics.DrawString($"Combo: x{combo}", scoreFont, New SolidBrush(Color.Yellow), 20, 80)
                ' Draw combo timer bar below the text
                Dim comboBarWidth As Integer = 100
                Dim comboBarHeight As Integer = 8
                Dim comboProgress As Single = comboTimer / comboMaxTime
                Dim barY As Integer = 105 ' Position below the combo text
                
                ' Background bar
                e.Graphics.FillRectangle(New SolidBrush(Color.DarkGray), 20, barY, comboBarWidth, comboBarHeight)
                ' Remaining time bar
                e.Graphics.FillRectangle(New SolidBrush(Color.Yellow), 20, barY, CInt(comboBarWidth * comboProgress), comboBarHeight)
                
                ' Add a border around the bar
                e.Graphics.DrawRectangle(New Pen(Color.White, 1), 20, barY, comboBarWidth, comboBarHeight)
            End If

            ' Draw lives
            For i As Integer = 1 To lives
                e.Graphics.FillEllipse(lifeIconBrush, 20 + (i * 30), If(combo > 1, 120, 80), 20, 20)
            Next

            ' Draw active power-up indicator (add after drawing lives)
            If hasPowerUp AndAlso powerUpType >= 0 AndAlso powerUpType < powerUpBrushes.Length Then
                ' Draw power-up icon
                Dim powerUpY As Integer = If(combo > 1, 150, 110)
                e.Graphics.FillEllipse(powerUpBrushes(powerUpType), 20, powerUpY, 20, 20)

                ' Draw power-up timer bar
                Dim barWidth As Integer = 100
                Dim barHeight As Integer = 10
                Dim remainingWidth As Integer = CInt(barWidth * (powerUpTimer / 600.0))

                ' Background bar
                e.Graphics.FillRectangle(New SolidBrush(Color.DarkGray), 50, powerUpY + 5, barWidth, barHeight)
                ' Remaining time bar
                e.Graphics.FillRectangle(powerUpBrushes(powerUpType), 50, powerUpY + 5, remainingWidth, barHeight)
            End If

            ' Draw game over message
            If isGameOver Then
                Dim gameOverText As String = "GAME OVER"
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

            ' Draw pause state
            If isPaused Then
                Dim pauseText As String = "PAUSED"
                Dim textSize = e.Graphics.MeasureString(pauseText, gameOverFont)
                Dim x As Single = (Me.ClientSize.Width - textSize.Width) / 2
                Dim y As Single = (Me.ClientSize.Height - textSize.Height) / 2
                e.Graphics.DrawString(pauseText, gameOverFont, scoreBrush, x, y)
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
                        Me.Close()
                End Select
                Return
            End If

            ' Track key states
            Select Case e.KeyCode
                Case Keys.Left
                    leftPressed = True
                Case Keys.Right
                    rightPressed = True
                Case Keys.Space
                    spacePressed = True
                Case Keys.P, Keys.Escape
                    isPaused = Not isPaused
                    If isPaused Then
                        GameTimer.Enabled = False
                    Else
                        GameTimer.Enabled = True
                    End If
            End Select
        Catch ex As Exception
            Debug.WriteLine($"Error in OnKeyDown: {ex.Message}")
        End Try
    End Sub

    Protected Overrides Sub OnKeyUp(e As KeyEventArgs)
        Try
            MyBase.OnKeyUp(e)

            ' Release key states
            Select Case e.KeyCode
                Case Keys.Left
                    leftPressed = False
                Case Keys.Right
                    rightPressed = False
                Case Keys.Space
                    spacePressed = False
            End Select
        Catch ex As Exception
            Debug.WriteLine($"Error in OnKeyUp: {ex.Message}")
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
            GameTimer.Dispose()
        Catch ex As Exception
            Debug.WriteLine($"Error in OnClosing: {ex.Message}")
        End Try
    End Sub
End Class
