﻿Imports HoMIDom
Imports HoMIDom.HoMIDom.Server
Imports HoMIDom.HoMIDom.Device
Imports System.IO

' Driver Foobar multiroom
' Auteur : Seb
' Date : 10/02/2011

''' <summary>Class Foobar le device doit donner par le biais de son adresse l'emplacement de l'executable de Foobar</summary>
''' <remarks></remarks>
<Serializable()> Public Class Driver_Foobar
    Implements HoMIDom.HoMIDom.IDriver

#Region "Variables génériques"
    '!!!Attention les variables ci-dessous doivent avoir une valeur par défaut obligatoirement
    'aller sur l'adresse http://www.somacon.com/p113.php pour avoir un ID
    Dim _ID As String = "9C3E7696-34F7-11E0-AEB0-53DBDED72085"
    Dim _Nom As String = "Foobar"
    Dim _Enable As String = False
    Dim _Description As String = "Multiroom audio Foobar"
    Dim _StartAuto As Boolean = False
    Dim _Protocol As String = "AUDIO"
    Dim _IsConnect As Boolean = False
    Dim _IP_TCP As String = ""
    Dim _Port_TCP As String = ""
    Dim _IP_UDP As String = ""
    Dim _Port_UDP As String = ""
    Dim _Com As String = ""
    Dim _Refresh As Integer = 0
    Dim _Modele As String = "Foobar"
    Dim _Version As String = "1.0"
    Dim _Picture As String = "audio.png"
    Dim _Server As HoMIDom.HoMIDom.Server
    Dim _Device As HoMIDom.HoMIDom.Device
    Dim _DeviceSupport As New ArrayList
    Dim _Parametres As New ArrayList
    Dim MyTimer As New Timers.Timer
#End Region

#Region "Variables Internes"

#End Region

#Region "Propriétés génériques"
    Public Property COM() As String Implements HoMIDom.HoMIDom.IDriver.COM
        Get
            Return _Com
        End Get
        Set(ByVal value As String)
            _Com = value
        End Set
    End Property
    Public ReadOnly Property Description() As String Implements HoMIDom.HoMIDom.IDriver.Description
        Get
            Return _Description
        End Get
    End Property
    Public ReadOnly Property DeviceSupport() As System.Collections.ArrayList Implements HoMIDom.HoMIDom.IDriver.DeviceSupport
        Get
            Return _DeviceSupport
        End Get
    End Property
    Public Property Parametres() As System.Collections.ArrayList Implements HoMIDom.HoMIDom.IDriver.Parametres
        Get
            Return _Parametres
        End Get
        Set(ByVal value As System.Collections.ArrayList)
            _Parametres = value
        End Set
    End Property
    Public Event DriverEvent(ByVal DriveName As String, ByVal TypeEvent As String, ByVal Parametre As Object) Implements HoMIDom.HoMIDom.IDriver.DriverEvent
    Public Property Enable() As Boolean Implements HoMIDom.HoMIDom.IDriver.Enable
        Get
            Return _Enable
        End Get
        Set(ByVal value As Boolean)
            _Enable = value
        End Set
    End Property
    Public ReadOnly Property ID() As String Implements HoMIDom.HoMIDom.IDriver.ID
        Get
            Return _ID
        End Get
    End Property
    Public Property IP_TCP() As String Implements HoMIDom.HoMIDom.IDriver.IP_TCP
        Get
            Return _IP_TCP
        End Get
        Set(ByVal value As String)
            _IP_TCP = value
        End Set
    End Property
    Public Property IP_UDP() As String Implements HoMIDom.HoMIDom.IDriver.IP_UDP
        Get
            Return _IP_UDP
        End Get
        Set(ByVal value As String)
            _IP_UDP = value
        End Set
    End Property
    Public ReadOnly Property IsConnect() As Boolean Implements HoMIDom.HoMIDom.IDriver.IsConnect
        Get
            Return _IsConnect
        End Get
    End Property
    Public ReadOnly Property Modele() As String Implements HoMIDom.HoMIDom.IDriver.Modele
        Get
            Return _Modele
        End Get
    End Property
    Public ReadOnly Property Nom() As String Implements HoMIDom.HoMIDom.IDriver.Nom
        Get
            Return _Nom
        End Get
    End Property
    Public Property Picture() As String Implements HoMIDom.HoMIDom.IDriver.Picture
        Get
            Return _Picture
        End Get
        Set(ByVal value As String)
            _Picture = value
        End Set
    End Property
    Public Property Port_TCP() As Object Implements HoMIDom.HoMIDom.IDriver.Port_TCP
        Get
            Return _Port_TCP
        End Get
        Set(ByVal value As Object)
            _Port_TCP = value
        End Set
    End Property
    Public Property Port_UDP() As String Implements HoMIDom.HoMIDom.IDriver.Port_UDP
        Get
            Return _Port_UDP
        End Get
        Set(ByVal value As String)
            _Port_UDP = value
        End Set
    End Property
    Public ReadOnly Property Protocol() As String Implements HoMIDom.HoMIDom.IDriver.Protocol
        Get
            Return _Protocol
        End Get
    End Property
    Public Property Refresh() As Integer Implements HoMIDom.HoMIDom.IDriver.Refresh
        Get
            Return _Refresh
        End Get
        Set(ByVal value As Integer)
            _Refresh = value
        End Set
    End Property
    Public Property Server() As HoMIDom.HoMIDom.Server Implements HoMIDom.HoMIDom.IDriver.Server
        Get
            Return _Server
        End Get
        Set(ByVal value As HoMIDom.HoMIDom.Server)
            _Server = value
        End Set
    End Property
    Public ReadOnly Property Version() As String Implements HoMIDom.HoMIDom.IDriver.Version
        Get
            Return _Version
        End Get
    End Property
    Public Property StartAuto() As Boolean Implements HoMIDom.HoMIDom.IDriver.StartAuto
        Get
            Return _StartAuto
        End Get
        Set(ByVal value As Boolean)
            _StartAuto = value
        End Set
    End Property
#End Region

#Region "Fonctions génériques"

    ''' <summary>Démarrer le du driver</summary>
    ''' <remarks></remarks>
    Public Sub Start() Implements HoMIDom.HoMIDom.IDriver.Start
        Try
            _IsConnect = True
            _Server.Log(TypeLog.INFO, TypeSource.DRIVER, "FOOBAR", "Driver " & Me.Nom & " démarré")
        Catch ex As Exception
            _Server.Log(TypeLog.ERREUR, TypeSource.DRIVER, "FOOBAR Start", ex.Message)
        End Try
    End Sub

    ''' <summary>Arrêter le du driver</summary>
    ''' <remarks></remarks>
    Public Sub [Stop]() Implements HoMIDom.HoMIDom.IDriver.Stop
        Try
            _IsConnect = False
            _Server.Log(TypeLog.INFO, TypeSource.DRIVER, "FOOBAR", "Driver " & Me.Nom & " arrêté")
        Catch ex As Exception
            _Server.Log(TypeLog.ERREUR, TypeSource.DRIVER, "FOOBAR Stop", ex.Message)
        End Try
    End Sub

    ''' <summary>Re-Démarrer le du driver</summary>
    ''' <remarks></remarks>
    Public Sub Restart() Implements HoMIDom.HoMIDom.IDriver.Restart
        [Stop]()
        Start()
    End Sub

    ''' <summary>Intérroger un device</summary>
    ''' <param name="Objet">Objet représetant le device à interroger</param>
    ''' <remarks>pas utilisé</remarks>
    Public Sub Read(ByVal Objet As Object) Implements HoMIDom.HoMIDom.IDriver.Read
        Try
            If _Enable = False Then Exit Sub
        Catch ex As Exception
            _Server.Log(TypeLog.ERREUR, TypeSource.DRIVER, "FOOBAR Read", ex.Message)
        End Try
    End Sub

    ''' <summary>Commander un device</summary>
    ''' <param name="Objet">Objet représetant le device à interroger</param>
    ''' <param name="Commande">La commande à passer</param>
    ''' <param name="Parametre1"></param>
    ''' <param name="Parametre2"></param>
    ''' <remarks></remarks>
    Public Sub Write(ByVal Objet As Object, ByVal Commande As String, Optional ByVal Parametre1 As Object = Nothing, Optional ByVal Parametre2 As Object = Nothing) Implements HoMIDom.HoMIDom.IDriver.Write
        Try
            If _Enable = False Then Exit Sub
            If Objet.type = "AUDIO" Then
                If File.Exists(Objet.adresse1) = False Then
                    _Server.Log(TypeLog.ERREUR, TypeSource.DRIVER, "FOOBAR", "Le fichier executable foobar n'existe pas")
                    Exit Sub
                End If
                Select Case UCase(Commande)
                    Case "PLAYAUDIO"
                        If Objet.Fichier = "" Then Exit Sub
                        Dim ProcId As Object
                        ProcId = Shell(Objet.Adresse1 & " /hide", AppWinStyle.Hide)
                        System.Threading.Thread.Sleep(1000)
                        ProcId = Shell(Objet.Adresse1 & " /add " & Objet.Fichier, AppWinStyle.Hide)
                        System.Threading.Thread.Sleep(3000)
                        ProcId = Shell(Objet.Adresse1 & " /play", AppWinStyle.Hide)
                        Objet.Value = "PLAY"
                    Case "PAUSEAUDIO"
                        Dim ProcId As Object
                        ProcId = Shell(Objet.Adresse1 & " /pause", AppWinStyle.Hide)
                        Objet.Value = "PAUSE"
                    Case "STOPAUDIO"
                        Dim ProcId As Object
                        ProcId = Shell(Objet.Adresse1 & " /command:Clear", AppWinStyle.Hide)
                        System.Threading.Thread.Sleep(500)
                        ProcId = Shell(Objet.Adresse1 & " /stop", AppWinStyle.Hide)
                        System.Threading.Thread.Sleep(500)
                        ProcId = Shell(Objet.Adresse1 & " /exit", AppWinStyle.Hide)
                        Objet.Value = "STOP"
                    Case "RANDOMAUDIO"
                        Dim ProcId As Object
                        ProcId = Shell(Objet.Adresse1 & " /random", AppWinStyle.Hide)
                        Objet.Value = "RANDOM"
                    Case "NEXTAUDIO"
                        Dim ProcId As Object
                        ProcId = Shell(Objet.Adresse1 & " /next", AppWinStyle.Hide)
                        Objet.Value = "NEXT"
                    Case "PREVIOUSAUDIO"
                        Dim ProcId As Object
                        ProcId = Shell(Objet.Adresse1 & " /previous", AppWinStyle.Hide)
                        Objet.Value = "PREVIOUS"
                    Case "VOLUMEDOWNAUDIO"
                        Dim ProcId As Object
                        ProcId = Shell(Objet.Adresse1 & " /Volume Down", AppWinStyle.Hide)
                        Objet.Value = "VOLUME DOWN"
                    Case "VOLUMEUPAUDIO"
                        Dim ProcId As Object
                        ProcId = Shell(Objet.Adresse1 & " /Volume Up", AppWinStyle.Hide)
                        Objet.Value = "VOLUME UP"
                    Case "VOLUMEMUTEAUDIO"
                        Dim ProcId As Object
                        ProcId = Shell(Objet.Adresse1 & " /Volume mute", AppWinStyle.Hide)
                        Objet.Value = "VOLUME MUTE"
                    Case Else
                        _Server.Log(TypeLog.INFO, TypeSource.DRIVER, "FOOBAR", "Commande inconnue:" & Commande)
                End Select
            Else
                _Server.Log(TypeLog.ERREUR, TypeSource.DRIVER, "FOOBAR", "Impossible d'envoyer un code IR pour un type de device autre que AUDIO")
            End If
        Catch ex As Exception
            _Server.Log(TypeLog.ERREUR, TypeSource.DRIVER, "FOOBAR Write", ex.Message)
        End Try
    End Sub

    ''' <summary>Fonction lancée lors de la suppression d'un device</summary>
    ''' <param name="DeviceId">Objet représetant le device à interroger</param>
    ''' <remarks></remarks>
    Public Sub DeleteDevice(ByVal DeviceId As String) Implements HoMIDom.HoMIDom.IDriver.DeleteDevice
        Try

        Catch ex As Exception
            _Server.Log(TypeLog.ERREUR, TypeSource.DRIVER, "FOOBAR DeleteDevice", ex.Message)
        End Try
    End Sub

    ''' <summary>Fonction lancée lors de l'ajout d'un device</summary>
    ''' <param name="DeviceId">Objet représetant le device à interroger</param>
    ''' <remarks></remarks>
    Public Sub NewDevice(ByVal DeviceId As String) Implements HoMIDom.HoMIDom.IDriver.NewDevice
        Try

        Catch ex As Exception
            _Server.Log(TypeLog.ERREUR, TypeSource.DRIVER, "FOOBAR NewDevice", ex.Message)
        End Try
    End Sub

    ''' <summary>Creation d'un objet de type</summary>
    ''' <remarks></remarks>
    Public Sub New()
        Try
            'liste des devices compatibles
            _DeviceSupport.Add(ListeDevices.AUDIO)

            'Parametres avancés
            Dim x As New HoMIDom.HoMIDom.Driver.Parametre
            x.Nom = "test"
            x.Description = "Description"
            _Parametres.Add(x)
        Catch ex As Exception
            _Server.Log(TypeLog.ERREUR, TypeSource.DRIVER, "FOOBAR New", ex.Message)
        End Try
    End Sub

    ''' <summary>Si refresh >0 gestion du timer</summary>
    ''' <remarks>PAS UTILISE CAR IL FAUT LANCER UN TIMER QUI LANCE/ARRETE CETTE FONCTION dans Start/Stop</remarks>
    Private Sub TimerTick()

    End Sub

#End Region

#Region "Fonctions internes"

#End Region

End Class
