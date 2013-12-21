﻿'#define LOG
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports System.Xml


Namespace ZibaseDllvb
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    ' Changelog - Dll de communication Zibase
    '
    '''
    '' v1.0 - 05/2010 - Version initiale
    '' v1.1 - 09/2010 - Ajout de type de données (xse, vs...)
    '' v1.2 - 12/2010 - Possibilité de choisir les paramètres d'association d'une base
    ''                - Correction d'un bug sur les codes unités >10 dans Send Command
    ''                - Gestion des variables, calendriers et état X10
    ''                - Meilleur détection des zibases dès le démarrage
    ''                - Gestion des virtuals probe !!!!!
    ''                - Gestion de la liste des scénarios et des capteurs depuis la plateforme
    ' v1.3
    '                 - GetCalendar(int) returned GetVar(int) => Fixed
    '                 - lev field is now correct (with comma)
    '                 - The sdk caller does not need to implement ISynchronizedInvoke anymore
    '                 - New protocols  PROTOCOL_X2D868_INTER_SHUTTER,PROTOCOL_XDD868_PILOT_WIRE,PROTOCOL_XDD868_BOILER_AC

    Public Class ZiBase
        Public Structure SensorInfo
            Public sHSName As String
            Public sName As String
            Public sType As String
            Public sID As String
            Public dwValue As Long
            Public sValue As String
            Public sHTMLValue As String
            Public sDevice As String
            Public sDate As DateTime
        End Structure
        Public Structure ZibaseInfo
            Public sLabelBase As String
            Public lIpAddress As Long
            Public sToken As String

            Public Function GetIPAsString() As [String]
                Dim ip As String = String.Empty
                For i As Integer = 0 To 3
                    Dim num As Integer = CInt(Math.Truncate(lIpAddress / Math.Pow(256, (3 - i))))
                    lIpAddress = lIpAddress - CLng(Math.Truncate(num * Math.Pow(256, (3 - i))))
                    If i = 0 Then
                        ip = num.ToString()
                    Else
                        ip = ip & "." & num.ToString()
                    End If
                Next
                Return ip
            End Function
        End Structure
#Region "Delegates"

        Public Delegate Sub NewSensorDetectedEventHandler(seInfo As SensorInfo)

        Public Delegate Sub NewZibaseDetectedEventHandler(zbInfo As ZibaseInfo)

        Public Delegate Sub UpdateSensorInfoEventHandler(seInfo As SensorInfo)

        Public Delegate Sub WriteMessageEventHandler(sMsg As String, level As Integer)

#End Region

#Region "Protocol enum"

        Public Enum Protocol
            PROTOCOL_BROADCAST = 0
            PROTOCOL_VISONIC433 = 1
            PROTOCOL_VISONIC868 = 2
            PROTOCOL_CHACON = 3
            PROTOCOL_DOMIA = 4
            PROTOCOL_X10 = 5
            PROTOCOL_ZWAVE = 6
            PROTOCOL_RFS10 = 7
            PROTOCOL_X2D433 = 8
            PROTOCOL_X2D868 = 9
            PROTOCOL_X2D868_INTER_SHUTTER = 10
            PROTOCOL_XDD868_PILOT_WIRE
            PROTOCOL_XDD868_BOILER_AC
        End Enum

#End Region

#Region "State enum"

        Public Enum State
            STATE_OFF = 0
            STATE_ON = 1
            STATE_DIM = 3
            STATE_ASSOC = 7
        End Enum

#End Region

#Region "VirtualProbeType enum"

        Public Enum VirtualProbeType
            TEMP_SENSOR = 0
            TEMP_HUM_SENSOR = 1
            POWER_SENSOR = 2
            WATER_SENSOR = 3
        End Enum

#End Region

#Region "ZibasePlateform enum"

        Public Enum ZibasePlateform
            ZODIANET = 0
            RESERVED = 1
            PLANETE_DOMOTIQUE = 2
            DOMADOO = 3
            ROBOPOLIS = 4
        End Enum

#End Region

        Public Const MSG_INFO As Integer = 0
        Public Const MSG_DEBUG As Integer = 1
        Public Const MSG_DEBUG_NOLOG As Integer = 2
        Public Const MSG_WARNING As Integer = 3

        Public Const MSG_ERROR As Integer = 4
        Private Const CMD_READ_VAR As Integer = 0
        Private Const CMD_TYPE_WRITE_VAR As Integer = 1
        Private Const CMD_READ_CAL As Integer = 2
        Private Const CMD_WRITE_CAL As Integer = 3

        Private Const CMD_READ_X10 As Integer = 4
        Private Const DOMO_EVENT_ACTION_OREGON_SIGNAL_32B_SENSOR_CODE As Integer = 17

        Private Const DOMO_EVENT_ACTION_OWL_SIGNAL_32B_SENSOR_CODE As Integer = 20
        Private ReadOnly _SensorList As New Dictionary(Of [String], SensorInfo)()
        Private ReadOnly m_Server As New IPEndPoint(IPAddress.Any, 0)
        Private ReadOnly m_Zbs As New ZBClass
        Private ReadOnly m_ZibaseList As New Dictionary(Of [String], ZibaseInfo)()
        Private m_AutoSearch As Boolean = True
        Private m_EndThread As Boolean
        Private m_ThreadSearch As Thread
        Private m_ThreadZibase As Thread

        Public Event WriteMessage As WriteMessageEventHandler

        Public Event UpdateSensorInfo As UpdateSensorInfoEventHandler

        Public Event NewZibaseDetected As NewZibaseDetectedEventHandler

        Public Event NewSensorDetected As NewSensorDetectedEventHandler


        'private String LogFilePath = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location),"ZibaseLog.txt");
        Private LogFilePath As [String] = Path.Combine("c:\", "ZibaseLog.txt")
        Private Sub LOG(log__1 As [String])
#If LOG Then
			File.AppendAllText(LogFilePath, log__1 & vbCr & vbLf)
#End If
        End Sub

        Public Sub StartZB(Optional bAutoSearch As Boolean = True, Optional dwPort As UInt32 = 0)
            m_AutoSearch = bAutoSearch

            m_Server.Port = CInt(dwPort)
            m_ThreadSearch = New Thread(AddressOf ThreadSearch)
            m_ThreadZibase = New Thread(AddressOf ThreadZibase)
            m_ThreadZibase.Start()
        End Sub

        Public Sub StopZB()
            m_EndThread = True
        End Sub


        Public Sub RestartZibaseSearch()
            RaiseEvent WriteMessage("Search for Zibase", MSG_DEBUG)
            m_Zbs.BrowseForZibase()
        End Sub

        Public Function GetSensorInfo(sID As String, sType As String) As SensorInfo
            Dim functionReturnValue As SensorInfo = Nothing
            If (_SensorList.Keys.Contains(sID & sType)) Then
                functionReturnValue = _SensorList(sID & sType)
            End If
            Return functionReturnValue
        End Function

        Public Sub SetServerPort(Port As Integer)
            m_Server.Port = Port
            m_Zbs.SetServerPort(CUInt(Port))
        End Sub

        Public Sub AddZibase(sZibaseIP As String, sLocalIP As String)
            Dim sNewZB As String = Nothing

            sNewZB = m_Zbs.InitZapi(sZibaseIP, sLocalIP)

            If (Not String.IsNullOrEmpty(sNewZB)) Then
                Dim IpAddr As IPAddress = Nothing

                IpAddr = IPAddress.Parse(sZibaseIP)
                Dim temp As Byte() = IpAddr.GetAddressBytes()
                Array.Reverse(temp)

                AddZibaseToCollection(sNewZB, BitConverter.ToUInt32(temp, 0))
            End If
        End Sub


        Private Sub ThreadSearch()
            ' Effectue une activation de l'api Zibase sur toute les Zibases du réseau
            RaiseEvent WriteMessage("Search for Zibase", MSG_DEBUG)
            m_Zbs.BrowseForZibase()
        End Sub

        Private Sub ThreadZibase()
            Dim Sck As Socket = Nothing

            Try
                RaiseEvent WriteMessage("Start running thread", MSG_DEBUG)

                Thread.Sleep(1000)

                Sck = New Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)

                If (m_Server.Port = 0) Then
                    m_Server.Port = 17100
                End If

                For i As Integer = 0 To 50
                    Try
                        Sck.Bind(m_Server)

                        ' TODO: might not be correct. Was : Exit For
                        Exit Try
                    Catch ex As Exception
                        ' Exception indiquant un port déjà utilisé
                        If DirectCast(ex, SocketException).SocketErrorCode = SocketError.AddressAlreadyInUse Then
                            RaiseEvent WriteMessage("IP Address and Port already in use. Try next port : " & (m_Server.Port + 1), MSG_DEBUG)
                        End If

                        m_Server.Port = m_Server.Port + 1
                    End Try
                Next

                m_Zbs.SetServerPort(CUInt(m_Server.Port))

                Sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 200)

                If (m_AutoSearch) Then
                    m_ThreadSearch.Start()
                End If

                While Not m_EndThread
                    Try
                        If (Sck.Available > 0) Then
                            Dim rBuff = New Byte(Sck.Available) {}
                            Sck.Receive(rBuff)

                            AfterReceive(rBuff)
                        End If
                    Catch generatedExceptionName As Exception
                    End Try

                    Thread.Sleep(5)
                End While
                'General Exception received--add code here to analyze the exception. A messagebox would be one idea.
            Catch ex As Exception
            End Try

            If (Sck IsNot Nothing) Then
                Sck.Close()
            End If
            'Always close the socket when done.
        End Sub

        ' Permet d'extraire une valeur de la chaine renvoyé par la Zibase
        Private Function GetValue(sStr As String, sName As String) As String
            Dim functionReturnValue As String = Nothing

            If sStr.Contains("<" & sName & ">") Then
                Dim iStart As Integer = sStr.IndexOf("<" & sName & ">") + Strings.Len("<" & sName & ">")
                Dim iLen As Integer = sStr.IndexOf("</" & sName & ">") - iStart
                functionReturnValue = sStr.Substring(iStart, iLen)
            Else
                functionReturnValue = ""
            End If
            Return functionReturnValue
        End Function


        Private Sub AddZibaseToCollection(sZibaseName As String, lIpAddress As Long)
            Dim seInfo = New SensorInfo()

            If (String.IsNullOrEmpty(sZibaseName)) Then
                sZibaseName = "Unknown"
            End If

            If (Not m_ZibaseList.Keys.Contains(sZibaseName)) Then
                Dim zb = New ZibaseInfo() With { _
                 .sLabelBase = sZibaseName, _
                 .lIpAddress = lIpAddress _
                }
                m_ZibaseList.Add(sZibaseName, zb)
                RaiseEvent NewZibaseDetected(zb)
                RaiseEvent WriteMessage("New Zibase Detected : " & sZibaseName, MSG_INFO)

                ' Creation d'un sensor virtuel pour la detection de l'availability de la Zibase
                seInfo.sName = "Zibase State"
                seInfo.sID = sZibaseName
                seInfo.sType = "lnk"
                seInfo.sValue = "Online"
                seInfo.sHTMLValue = "Online"
                seInfo.dwValue = 2

                If (_SensorList.Keys.Contains(sZibaseName & "lnk")) Then
                    seInfo.sHSName = _SensorList(sZibaseName & "lnk").sHSName
                    seInfo.sDevice = _SensorList(sZibaseName & "lnk").sDevice

                    _SensorList(sZibaseName & "lnk") = seInfo
                Else
                    _SensorList.Add(sZibaseName & "lnk", seInfo)
                    RaiseEvent NewSensorDetected(seInfo)
                End If
                RaiseEvent UpdateSensorInfo(seInfo)
            End If
        End Sub

        ' Traitement des messages envoyés dans
        Private Sub AfterReceive(_data As Byte())
            Try
                Dim seInfo = New SensorInfo()


                If _data.Length >= 70 And _data(5) = 3 Then
                    ' On récupére d'abord les info générales sur le message
                    m_Zbs.SetData(_data)

                    Dim sZibaseName As String = Encoding.[Default].GetString(m_Zbs.label_base)
                    Dim iPos As Integer = Strings.InStr(sZibaseName, Strings.Chr(0))
                    If iPos > 0 Then
                        sZibaseName = sZibaseName.Substring(0, iPos - 1)
                    End If
                    ' Strings.Left(sZibaseName, iPos - 1);
                    AddZibaseToCollection(sZibaseName, m_Zbs.my_ip)

                    Dim s As String = ""
                    Dim i As Integer = 0
                    For i = 70 To _data.Length - 2
                        s = s & Strings.Chr(_data(i))
                    Next

                    RaiseEvent WriteMessage(sZibaseName & ":" & s, MSG_DEBUG)

                    LOG(s)

                    's = "Received radio ID (<rf>433Mhz</rf> Noise=<noise>2564</noise> Level=<lev>4.6</lev>/5 <dev>Oregon THWR288A-THN132N</dev> Ch=<ch>1</ch> T=<tem>+15.4</tem>°C (+59.7°F) Batt=<bat>Ok</bat>): <id>OS3930910721</id>";
                    If (s.Substring(0, 17).ToUpper() = "RECEIVED RADIO ID") Then
                        seInfo.sID = GetValue(s, "id")
                        seInfo.sName = GetValue(s, "dev")
                        seInfo.sDate = DateTime.Now

                        '#Region "Remote Control"

                        '| LPL ?? Information.IsNumeric(seInfo.sID.Substring(1).Replace("_OFF", ""))))
                        If (Strings.Asc(seInfo.sID(0)) >= Strings.Asc("A"c) AndAlso Strings.Asc(seInfo.sID(0)) <= Strings.Asc("P"c) AndAlso ([Char].IsNumber(seInfo.sID, 1))) Then
                            seInfo.sName = "Remote Control"

                            ' Traitement de la donnée reçu
                            ' On parcours la liste des passerelles
                            For i = 0 To 15
                                seInfo.sDevice = Strings.Chr(65 + i) & seInfo.sID.Substring(1)
                                seInfo.sValue = ""

                                Select Case seInfo.sID.IndexOf("_OFF")
                                    Case -1
                                        seInfo.dwValue = 2
                                        seInfo.sValue = "On"
                                        Exit Select
                                    Case Else
                                        seInfo.dwValue = 3
                                        seInfo.sValue = "Off"
                                        Exit Select
                                End Select

                                seInfo.sID = seInfo.sID.Replace("_OFF", "")

                                RaiseEvent UpdateSensorInfo(seInfo)

                                ' On remet l'id d'origine version Zibase
                                ' seInfo.sID = GetValue(s, "id").Replace("_OFF", "")
                            Next
                        End If

                        '#End Region

                        ' On modifie l'id pour Chacon et Visonic pour qui correspondent à l'actionneur (ON et OFF)
                        If (seInfo.sID.Substring(0, 2) = "CS") Then
                            seInfo.sID = "CS" & ((Convert.ToInt32(seInfo.sID.Substring(2), System.Globalization.CultureInfo.InvariantCulture) And Not &H10))
                            seInfo.sName = "Chacon"
                        End If

                        If (seInfo.sID.Substring(0, 2) = "VS") Then
                            seInfo.sID = "VS" & ((Convert.ToInt32(seInfo.sID.Substring(2), System.Globalization.CultureInfo.InvariantCulture)) And Not &HF)
                            seInfo.sName = "Visonic"
                        End If

                        If (seInfo.sID.Substring(0, 2) = "DX") Then
                            seInfo.sID = "DX" & seInfo.sID.Substring(2)
                            seInfo.sName = "X2D"
                        End If

                        If (seInfo.sID.Substring(0, 2) = "WS") Then
                            LOG(seInfo.sID)
                            seInfo.sID = "WS" & ((Convert.ToInt32(seInfo.sID.Substring(2), System.Globalization.CultureInfo.InvariantCulture)) And Not &HF)
                            seInfo.sName = "OWL"
                        End If

                        Dim sId As String = Nothing
                        Dim sValue As String = Nothing
                        Dim sType As String = Nothing

                        '#Region "XS Security Device"

                        If (seInfo.sID.Substring(0, 2) = "XS") Then
                            sValue = ((Convert.ToInt64(seInfo.sID.Substring(2), System.Globalization.CultureInfo.InvariantCulture)) And &HFF).ToString()
                            seInfo.sID = "XS" & ((Convert.ToInt64(seInfo.sID.Substring(2))) And Not &HFF)
                            seInfo.sName = "X10 Secured"

                            sType = "xse"
                            seInfo.sType = sType
                            seInfo.sDevice = ""

                            ' Declaration d'une variable de type état
                            If (Not String.IsNullOrEmpty(sValue)) Then
                                seInfo.dwValue = Convert.ToInt32(sValue, System.Globalization.CultureInfo.InvariantCulture)
                                Select Case seInfo.dwValue
                                    Case &H20, &H30
                                        seInfo.sValue = "ALERT"
                                        seInfo.sHTMLValue = "ALERT"
                                        Exit Select
                                    Case &H21, &H31
                                        seInfo.sValue = "NORMAL"
                                        seInfo.sHTMLValue = "NORMAL"
                                        Exit Select
                                    Case &H40
                                        seInfo.sValue = "ARM AWAY (max)"
                                        seInfo.sHTMLValue = "ARM AWAY (max)"
                                        Exit Select
                                    Case &H41, &H61
                                        seInfo.sValue = "DISARM"
                                        seInfo.sHTMLValue = "DISARM"
                                        Exit Select
                                    Case &H42
                                        seInfo.sValue = "SEC. LIGHT ON"
                                        seInfo.sHTMLValue = "SEC. LIGHT ON"
                                        Exit Select
                                    Case &H43
                                        seInfo.sValue = "SEC. LIGHT OFF"
                                        seInfo.sHTMLValue = "SEC. LIGHT OFF"
                                        Exit Select
                                    Case &H44
                                        seInfo.sValue = "PANIC"
                                        seInfo.sHTMLValue = "PANIC"
                                        Exit Select
                                    Case &H50
                                        seInfo.sValue = "ARM HOME"
                                        seInfo.sHTMLValue = "ARM HOME"
                                        Exit Select
                                    Case &H60
                                        seInfo.sValue = "ARM"
                                        seInfo.sHTMLValue = "ARM"
                                        Exit Select
                                    Case &H62
                                        seInfo.sValue = "LIGHTS ON"
                                        seInfo.sHTMLValue = "LIGHTS ON"
                                        Exit Select
                                    Case &H63
                                        seInfo.sValue = "LIGHTS OFF"
                                        seInfo.sHTMLValue = "LIGHTS OFF"
                                        Exit Select
                                    Case &H70
                                        seInfo.sValue = "ARM HOME (min)"
                                        seInfo.sHTMLValue = "ARM HOME (min)"
                                        Exit Select
                                End Select

                                sId = seInfo.sID

                                If (_SensorList.Keys.Contains(sId & sType)) Then
                                    seInfo.sHSName = _SensorList(sId & sType).sHSName
                                    seInfo.sDevice = _SensorList(sId & sType).sDevice

                                    _SensorList(sId & sType) = seInfo
                                Else
                                    _SensorList.Add(sId & sType, seInfo)
                                End If

                                RaiseEvent UpdateSensorInfo(seInfo)
                            End If
                        End If

                        '#End Region

                        '#Region "sta"

                        sId = seInfo.sID

                        sType = "sta"
                        seInfo.sType = sType
                        seInfo.sDevice = ""
                        sValue = GetValue(s, sType)

                        ' Declaration d'une variable de type état
                        If (Not String.IsNullOrEmpty(sValue)) Then
                            seInfo.sValue = sValue
                            seInfo.sHTMLValue = sValue

                            seInfo.dwValue = If(sValue = "ON", 2, 3)

                            If (_SensorList.Keys.Contains(sId & sType)) Then
                                seInfo.sHSName = _SensorList(sId & sType).sHSName
                                seInfo.sDevice = _SensorList(sId & sType).sDevice

                                _SensorList(sId & sType) = seInfo
                            Else
                                _SensorList.Add(sId & sType, seInfo)

                                RaiseEvent NewSensorDetected(seInfo)
                            End If

                            RaiseEvent UpdateSensorInfo(seInfo)
                        End If

                        '#End Region

                        '#Region "lev"

                        sType = "lev"
                        seInfo.sType = sType
                        seInfo.sDevice = ""
                        sValue = GetValue(s, sType)

                        ' Declaration d'une variable de type strength level
                        If (Not String.IsNullOrEmpty(sValue)) Then
                            seInfo.dwValue = CInt(Math.Truncate(Convert.ToDouble(sValue, System.Globalization.CultureInfo.InvariantCulture) * 10))
                            seInfo.sValue = (seInfo.dwValue / 10.0) & "/5"
                            seInfo.sHTMLValue = sValue

                            If (_SensorList.Keys.Contains(sId & sType)) Then
                                seInfo.sHSName = _SensorList(sId & sType).sHSName
                                seInfo.sDevice = _SensorList(sId & sType).sDevice

                                _SensorList(sId & sType) = seInfo
                            Else
                                _SensorList.Add(sId & sType, seInfo)

                                RaiseEvent NewSensorDetected(seInfo)
                            End If

                            RaiseEvent UpdateSensorInfo(seInfo)
                        End If

                        '#End Region

                        '#Region "temc"

                        sType = "temc"
                        seInfo.sType = sType
                        seInfo.sDevice = ""
                        sValue = GetValue(s, sType)

                        ' Declaration d'une variable de type consigne de température
                        If (Not String.IsNullOrEmpty(sValue)) Then
                            seInfo.dwValue = Convert.ToInt32(sValue, System.Globalization.CultureInfo.InvariantCulture)
                            seInfo.sValue = seInfo.dwValue & "°C"
                            seInfo.sHTMLValue = sValue

                            If (_SensorList.Keys.Contains(sId & sType)) Then
                                seInfo.sHSName = _SensorList(sId & sType).sHSName
                                seInfo.sDevice = _SensorList(sId & sType).sDevice

                                _SensorList(sId & sType) = seInfo
                            Else
                                _SensorList.Add(sId & sType, seInfo)

                                RaiseEvent NewSensorDetected(seInfo)
                                RaiseEvent NewSensorDetected(seInfo)
                            End If

                            RaiseEvent UpdateSensorInfo(seInfo)
                        End If

                        '#End Region

                        '#Region "kwh"

                        sType = "kwh"
                        seInfo.sType = sType
                        seInfo.sDevice = ""
                        sValue = GetValue(s, sType)

                        If (Not String.IsNullOrEmpty(sValue)) Then
                            LOG(DateTime.Now & " KWh :" & sValue)
                            seInfo.dwValue = CLng(Math.Truncate(Convert.ToDouble(sValue, CultureInfo.InvariantCulture) * 100))
                            seInfo.sValue = (seInfo.dwValue / 100.0) & " kWh"
                            seInfo.sHTMLValue = sValue
                            LOG(DateTime.Now & " Trace1")

                            If (_SensorList.Keys.Contains(sId & sType)) Then
                                seInfo.sHSName = _SensorList(sId & sType).sHSName
                                seInfo.sDevice = _SensorList(sId & sType).sDevice

                                _SensorList(sId & sType) = seInfo
                            Else
                                _SensorList.Add(sId & sType, seInfo)

                                RaiseEvent NewSensorDetected(seInfo)
                            End If

                            RaiseEvent UpdateSensorInfo(seInfo)
                        End If

                        '#End Region

                        '#Region "kw"

                        sType = "kw"
                        seInfo.sType = sType
                        seInfo.sDevice = ""
                        sValue = GetValue(s, sType)

                        If (Not String.IsNullOrEmpty(sValue)) Then
                            seInfo.dwValue = CLng(Math.Truncate(Convert.ToDouble(sValue, CultureInfo.InvariantCulture) * 100))
                            seInfo.sValue = (seInfo.dwValue / 100.0) & " kW"
                            seInfo.sHTMLValue = sValue

                            If (_SensorList.Keys.Contains(sId & sType)) Then
                                seInfo.sHSName = _SensorList(sId & sType).sHSName
                                seInfo.sDevice = _SensorList(sId & sType).sDevice

                                _SensorList(sId & sType) = seInfo
                            Else
                                _SensorList.Add(sId & sType, seInfo)

                                RaiseEvent NewSensorDetected(seInfo)
                            End If

                            RaiseEvent UpdateSensorInfo(seInfo)
                        End If

                        '#End Region

                        '#Region "tra"

                        sType = "tra"
                        seInfo.sType = sType
                        seInfo.sDevice = ""
                        sValue = GetValue(s, sType)

                        If (Not String.IsNullOrEmpty(sValue)) Then
                            seInfo.dwValue = Convert.ToInt32(sValue, System.Globalization.CultureInfo.InvariantCulture) * 100
                            seInfo.sValue = seInfo.dwValue & " mm"
                            seInfo.sHTMLValue = sValue

                            If (_SensorList.Keys.Contains(sId & sType)) Then
                                seInfo.sHSName = _SensorList(sId & sType).sHSName
                                seInfo.sDevice = _SensorList(sId & sType).sDevice

                                _SensorList(sId & sType) = seInfo
                            Else
                                _SensorList.Add(sId & sType, seInfo)

                                RaiseEvent NewSensorDetected(seInfo)
                            End If

                            RaiseEvent UpdateSensorInfo(seInfo)
                        End If

                        '#End Region

                        '#Region "cra"

                        sType = "cra"
                        seInfo.sType = sType
                        seInfo.sDevice = ""
                        sValue = GetValue(s, sType)

                        If (Not String.IsNullOrEmpty(sValue)) Then
                            seInfo.dwValue = Convert.ToInt32(sValue, System.Globalization.CultureInfo.InvariantCulture) * 100
                            seInfo.sValue = seInfo.dwValue & " mm/h"
                            seInfo.sHTMLValue = sValue

                            If (_SensorList.Keys.Contains(sId & sType)) Then
                                seInfo.sHSName = _SensorList(sId & sType).sHSName
                                seInfo.sDevice = _SensorList(sId & sType).sDevice

                                _SensorList(sId & sType) = seInfo
                            Else
                                _SensorList.Add(sId & sType, seInfo)

                                RaiseEvent NewSensorDetected(seInfo)
                            End If

                            RaiseEvent UpdateSensorInfo(seInfo)
                        End If

                        '#End Region

                        '#Region "awi"

                        sType = "awi"
                        seInfo.sType = sType
                        seInfo.sDevice = ""
                        sValue = GetValue(s, sType)

                        If (Not String.IsNullOrEmpty(sValue)) Then
                            seInfo.dwValue = CLng(Math.Truncate(Convert.ToDouble(sValue, CultureInfo.InvariantCulture) * 100))
                            seInfo.sValue = seInfo.dwValue & " m/s"
                            seInfo.sHTMLValue = sValue

                            If (_SensorList.Keys.Contains(sId & sType)) Then
                                seInfo.sHSName = _SensorList(sId & sType).sHSName
                                seInfo.sDevice = _SensorList(sId & sType).sDevice

                                _SensorList(sId & sType) = seInfo
                            Else
                                _SensorList.Add(sId & sType, seInfo)

                                RaiseEvent NewSensorDetected(seInfo)
                            End If

                            RaiseEvent UpdateSensorInfo(seInfo)
                        End If

                        '#End Region

                        '#Region "drt"

                        sType = "drt"
                        seInfo.sType = sType
                        seInfo.sDevice = ""
                        sValue = GetValue(s, sType)

                        If (Not String.IsNullOrEmpty(sValue)) Then
                            seInfo.dwValue = Convert.ToInt32(sValue, System.Globalization.CultureInfo.InvariantCulture) * 100
                            seInfo.sValue = seInfo.dwValue & " °"
                            seInfo.sHTMLValue = sValue

                            If (_SensorList.Keys.Contains(sId & sType)) Then
                                seInfo.sHSName = _SensorList(sId & sType).sHSName
                                seInfo.sDevice = _SensorList(sId & sType).sDevice

                                _SensorList(sId & sType) = seInfo
                            Else
                                _SensorList.Add(sId & sType, seInfo)

                                RaiseEvent NewSensorDetected(seInfo)
                            End If

                            RaiseEvent UpdateSensorInfo(seInfo)
                        End If

                        '#End Region

                        '#Region "uvl"

                        sType = "uvl"
                        seInfo.sType = sType
                        seInfo.sDevice = ""
                        sValue = GetValue(s, sType)

                        If (Not String.IsNullOrEmpty(sValue)) Then
                            seInfo.dwValue = Convert.ToInt32(sValue, System.Globalization.CultureInfo.InvariantCulture) * 100
                            seInfo.sValue = seInfo.dwValue & ""
                            seInfo.sHTMLValue = sValue

                            If (_SensorList.Keys.Contains(sId & sType)) Then
                                seInfo.sHSName = _SensorList(sId & sType).sHSName
                                seInfo.sDevice = _SensorList(sId & sType).sDevice

                                _SensorList(sId & sType) = seInfo
                            Else
                                _SensorList.Add(sId & sType, seInfo)

                                RaiseEvent NewSensorDetected(seInfo)
                            End If

                            RaiseEvent UpdateSensorInfo(seInfo)
                        End If

                        '#End Region

                        '#Region "bat"

                        sType = "bat"
                        seInfo.sType = sType
                        seInfo.sDevice = ""
                        sValue = GetValue(s, sType)

                        ' Declaration d'une variable de type battery
                        If (Not String.IsNullOrEmpty(sValue) And sValue <> "?") Then
                            seInfo.sValue = sValue
                            seInfo.sHTMLValue = sValue

                            seInfo.dwValue = If(sValue = "Low", 0, 1)

                            If (_SensorList.Keys.Contains(sId & sType)) Then
                                seInfo.sHSName = _SensorList(sId & sType).sHSName
                                seInfo.sDevice = _SensorList(sId & sType).sDevice

                                _SensorList(sId & sType) = seInfo
                            Else
                                _SensorList.Add(sId & sType, seInfo)

                                RaiseEvent NewSensorDetected(seInfo)
                            End If

                            RaiseEvent UpdateSensorInfo(seInfo)
                        End If

                        '#End Region

                        '#Region "tem"

                        ' Gestion du type de sonde
                        sType = "tem"
                        seInfo.sType = sType
                        seInfo.sDevice = ""
                        sValue = GetValue(s, sType)

                        ' Declaration d'une variable de type temperature
                        If (Not String.IsNullOrEmpty(sValue)) Then
                            seInfo.dwValue = CLng(Math.Truncate(Convert.ToDouble(sValue, CultureInfo.InvariantCulture) * 100))
                            seInfo.sValue = [String].Format("{0:0.0} °C", seInfo.dwValue / 100.0)
                            ' "#.#") + "°C";
                            seInfo.sHTMLValue = sValue

                            If (_SensorList.Keys.Contains(sId & sType)) Then
                                seInfo.sHSName = _SensorList(sId & sType).sHSName
                                seInfo.sDevice = _SensorList(sId & sType).sDevice

                                _SensorList(sId & sType) = seInfo
                            Else
                                _SensorList.Add(sId & sType, seInfo)

                                RaiseEvent NewSensorDetected(seInfo)
                            End If

                            RaiseEvent UpdateSensorInfo(seInfo)
                        End If

                        '#End Region

                        '#Region "hum"

                        sType = "hum"
                        seInfo.sType = sType
                        seInfo.sDevice = ""
                        sValue = GetValue(s, sType)

                        ' Declaration d'une variable de type humidity
                        If (Not String.IsNullOrEmpty(sValue)) Then
                            seInfo.dwValue = Convert.ToInt32(sValue, System.Globalization.CultureInfo.InvariantCulture)
                            seInfo.sValue = seInfo.dwValue & "%"
                            seInfo.sHTMLValue = sValue

                            If (_SensorList.Keys.Contains(sId & sType)) Then
                                seInfo.sHSName = _SensorList(sId & sType).sHSName
                                seInfo.sDevice = _SensorList(sId & sType).sDevice

                                _SensorList(sId & sType) = seInfo
                            Else
                                _SensorList.Add(sId & sType, seInfo)

                                RaiseEvent NewSensorDetected(seInfo)
                            End If

                            RaiseEvent UpdateSensorInfo(seInfo)

                            '#End Region
                        End If
                    End If
                End If
            Catch ex As Exception
                LOG(ex.Message)
            End Try
        End Sub

        Public Sub SendCommand(sAddress As String, iState As State, Optional iDim As Integer = 0, Optional iProtocol As Protocol = Protocol.PROTOCOL_CHACON, Optional iNbBurst As Integer = 1)
            SendCommand("", sAddress, iState, iDim, iProtocol, iNbBurst)
        End Sub

        Public Sub SendCommand(sZibaseName As String, sAddress As String, iState As State, Optional iDim As Integer = 0, Optional iProtocol As Protocol = Protocol.PROTOCOL_CHACON, Optional iNbBurst As Integer = 1)
            If (Strings.Len(sAddress) < 2) Then
                Return
            End If

            Dim ZBS = New ZBClass()

            ZBS.header = ZBS.GetBytesFromString("ZSIG")
            ZBS.command = 11
            ZBS.alphacommand = ZBS.GetBytesFromString("SendX10")
            ZBS.label_base = ZBS.GetBytesFromString("")

            ZBS.serial = 0
            ZBS.param1 = 0

            If iState = State.STATE_DIM And iDim = 0 Then
                iState = State.STATE_OFF
            End If

            Select Case iState
                Case State.STATE_OFF
                    ZBS.param2 = 0
                    Exit Select
                Case State.STATE_ON
                    ZBS.param2 = 1
                    Exit Select
                Case State.STATE_DIM
                    ZBS.param2 = 3
                    Exit Select
            End Select

            ' DEFAULT BROADCAST (RF X10, CHACON, DOMIA) : 0
            ' VISONIC433:   1,          ( frequency : device RF LOW, 310...418Mhz band))
            ' VISONIC868:   2,          (  frequency :  device RF HIGH, 868 Mhz Band)
            ' CHACON (32B) (ChaconV2) :  3
            ' DOMIA (24B) ( =Chacon V1 + low cost shit-devices):    4
            ' RF X10 :    5
            ZBS.param2 = CUInt(ZBS.param2 Or ((CInt(iProtocol) And &HFF) << 8))

            ' Dim
            If (iState = State.STATE_DIM) Then
                ZBS.param2 = CUInt(ZBS.param2 Or ((iDim And &HFF) << 16))
            End If

            If (iNbBurst <> 1) Then
                ZBS.param2 = CUInt(ZBS.param2 Or ((iNbBurst And &HFF) << 24))
            End If

            Dim sHouse As String = Strings.Mid(sAddress, 1, 1)
            Dim sCode As String = Strings.Mid(sAddress, 2)

            ZBS.param3 = CUInt(Convert.ToInt32(sCode)) - 1
            ZBS.param4 = CUInt(Strings.Asc(sHouse(0))) - 65

            SendToZibase(sZibaseName, ZBS)
        End Sub

        Public Sub ExecScript(sScript As String)
            ExecScript("", sScript)
        End Sub

        Public Sub RunScenario(sZibaseName As String, sName As String)
            ExecScript(sZibaseName, "lm [" & sName & "]")
        End Sub

        Public Sub RunScenario(sName As String)
            RunScenario("", sName)
        End Sub

        Public Sub RunScenario(sZibaseName As String, iNum As Integer)
            ExecScript(sZibaseName, "lm " & iNum)
        End Sub

        Public Sub RunScenario(iNum As Integer)
            RunScenario("", iNum)
        End Sub

        Public Sub ExecScript(sZibaseName As String, sScript As String)
            sScript = "cmd:" & sScript

            If (Strings.Len(sScript) > 96) Then
                Return
            End If

            Dim ZBS = New ZBClass()

            ZBS.header = ZBS.GetBytesFromString("ZSIG")
            ZBS.command = 16
            ZBS.alphacommand = ZBS.GetBytesFromString("SendCmd")
            ZBS.label_base = ZBS.GetBytesFromString("")

            ZBS.command_text = ZBS.GetBytesFromString(sScript)

            ZBS.serial = 0
            ZBS.param1 = 0
            ZBS.param2 = 0
            ZBS.param3 = 0
            ZBS.param4 = 0
            SendToZibase(sZibaseName, ZBS)
        End Sub


        Public Function GetVar(dwNumVar As UInt32) As UInt32
            Return GetVar("", dwNumVar)
        End Function

        Public Function GetVar(sZibaseName As String, dwNumVar As UInt32) As UInt32
            Dim ZBS = New ZBClass()

            ZBS.header = ZBS.GetBytesFromString("ZSIG")
            ZBS.command = 11
            ZBS.alphacommand = ZBS.GetBytesFromString("GetVar")
            ZBS.label_base = ZBS.GetBytesFromString("")

            ZBS.serial = 0

            ZBS.param1 = 5
            ZBS.param2 = 0
            ZBS.param3 = CMD_READ_VAR
            ZBS.param4 = dwNumVar

            Return SendToZibase(sZibaseName, ZBS)
        End Function

        Public Function GetX10State(house As Char, unit As Byte) As Boolean
            Return GetX10State("", house, unit)
        End Function

        Public Function GetX10State(sZibaseName As String, house As Char, unit As Byte) As Boolean
            Dim ZBS = New ZBClass()

            ZBS.header = ZBS.GetBytesFromString("ZSIG")
            ZBS.command = 11
            ZBS.alphacommand = ZBS.GetBytesFromString("GetX10")
            ZBS.label_base = ZBS.GetBytesFromString("")

            ZBS.serial = 0

            ZBS.param1 = 5
            ZBS.param2 = 0
            ZBS.param3 = CMD_READ_X10
            ZBS.param4 = CUInt((Strings.Asc(house) - Strings.Asc("A"c)) << 8) Or unit

            Return SendToZibase(sZibaseName, ZBS) = 1
        End Function


        Public Function GetCalendar(dwNumCal As UInt32) As UInt32
            Return GetCalendar("", dwNumCal)
        End Function


        Public Function SendToZibase(ZibaseName As [String], SendBuffer As ZBClass) As UInteger
            Dim dataRcv As Byte() = Nothing
            Dim ZibaseReceiveBuf = New ZBClass()
            Dim q As IEnumerable(Of ZibaseInfo) = (From c In m_ZibaseList Where c.Value.sLabelBase = ZibaseName OrElse [String].IsNullOrEmpty(ZibaseName) Select c.Value)

            If q.Any() Then
                Dim zInfo As ZibaseInfo = q.First()
                Dim ZibaseIP As [String] = zInfo.GetIPAsString()
                m_Zbs.UDPDataTransmit(SendBuffer.GetBytes(), dataRcv, ZibaseIP, 49999)
                ZibaseReceiveBuf.SetData(dataRcv)
                Return ZibaseReceiveBuf.param1
            End If
            Return 0
        End Function

        Public Function GetCalendar(sZibaseName As String, dwNumCal As UInt32) As UInt32
            Dim ZBS = New ZBClass()


            ZBS.header = ZBS.GetBytesFromString("ZSIG")
            ZBS.command = 11
            ZBS.alphacommand = ZBS.GetBytesFromString("GetCal")
            ZBS.label_base = ZBS.GetBytesFromString("")

            ZBS.serial = 0

            ZBS.param1 = 5
            ZBS.param2 = 0
            ZBS.param3 = CMD_READ_CAL
            ZBS.param4 = dwNumCal

            Return SendToZibase(sZibaseName, ZBS)
        End Function

        Public Sub SetVar(dwNumVar As UInt32, dwVal As UInt32)
            SetVar("", dwNumVar, dwVal)
        End Sub

        Public Sub SetVar(sZibaseName As String, dwNumVar As UInt32, dwVal As UInt32)
            Dim ZBS = New ZBClass()

            ZBS.header = ZBS.GetBytesFromString("ZSIG")
            ZBS.command = 11
            ZBS.alphacommand = ZBS.GetBytesFromString("SetVar")
            ZBS.label_base = ZBS.GetBytesFromString("")

            ZBS.serial = 0

            ZBS.param1 = 5
            ZBS.param2 = dwVal
            ZBS.param3 = CMD_TYPE_WRITE_VAR
            ZBS.param4 = dwNumVar

            SendToZibase(sZibaseName, ZBS)
        End Sub

        Public Sub SetCalendar(dwNumCal As UInt32, dwVal As UInt32)
            SetCalendar("", dwNumCal, dwVal)
        End Sub

        Public Sub SetCalendar(sZibaseName As String, dwNumCal As UInt32, dwVal As UInt32)
            Dim ZBS = New ZBClass()

            ZBS.header = ZBS.GetBytesFromString("ZSIG")
            ZBS.command = 11
            ZBS.alphacommand = ZBS.GetBytesFromString("SetCal")
            ZBS.label_base = ZBS.GetBytesFromString("")

            ZBS.serial = 0

            ZBS.param1 = 5
            ZBS.param2 = dwVal
            ZBS.param3 = CMD_WRITE_CAL
            ZBS.param4 = dwNumCal

            SendToZibase(sZibaseName, ZBS)
        End Sub

        Public Function GetCalendarAsString(dwNumCal As UInt32) As String
            Return GetCalendarAsString("", dwNumCal)
        End Function

        Public Function GetCalendarAsString(sZibaseName As String, dwNumCal As UInt32) As String
            Dim val As UInt32 = Nothing
            Dim sHour As String = Nothing
            Dim sDay As String = Nothing

            val = GetCalendar(sZibaseName, dwNumCal)

            sHour = ""
            sDay = ""

            For i As Integer = 0 To 30
                If (i <= 23) Then
                    If (val And 1) = 1 Then
                        sHour = sHour & "1"
                    Else
                        sHour = sHour & "0"
                    End If
                Else
                    If (val And 1) = 1 Then
                        sDay = sDay & "1"
                    Else
                        sDay = sDay & "0"
                    End If
                End If

                val = val >> 1
            Next

            Return sDay & ";" & sHour
        End Function

        Public Function GetCalendarFromString(sDay As String, sHour As String) As UInt32
            Dim val As UInt32 = Nothing

            ' On compléte les variables pour être sur du nombre de donneés
            sDay = sDay & "0000000"
            sHour = sHour & "000000000000000000000000"

            val = 0

            Dim i As Integer = 6
            While i >= 0
                If (sDay.ElementAt(i) = "1"c) Then
                    val = val Or 1
                End If
                val = val << 1
                i += -1
            End While
            i = 23
            While i >= 0
                If (sHour.ElementAt(i) = "1"c) Then
                    val = val Or 1
                End If
                If (i <> 0) Then
                    val = val << 1
                End If
                i += -1
            End While

            Return val
        End Function

        Public Sub SetVirtualProbeValue(dwSensorID As UInt32, SensorType As VirtualProbeType, dwValue1 As UInt32, dwValue2 As UInt32, dwLowBat As UInt32)
            SetVirtualProbeValue("", CUShort(dwSensorID), SensorType, dwValue1, dwValue2, dwLowBat)
        End Sub

        Public Sub SetVirtualProbeValue(sZibaseName As String, wSensorID As UInt16, SensorType As VirtualProbeType, dwValue1 As UInt32, dwValue2 As UInt32, dwLowBat As UInt32)
            Dim ZBS = New ZBClass()
            'ZBClass ZBSrcv = new ZBClass();
            Dim iSensorType As Integer = 0
            Dim dwSensorID As UInt32 = Nothing

            Select Case SensorType
                ' Simule un OWL
                Case VirtualProbeType.POWER_SENSOR
                    iSensorType = DOMO_EVENT_ACTION_OWL_SIGNAL_32B_SENSOR_CODE
                    dwSensorID = CUInt(&H2) << 16 Or wSensorID

                    Exit Select
                    ' Simule une THGR228
                Case VirtualProbeType.TEMP_HUM_SENSOR
                    iSensorType = DOMO_EVENT_ACTION_OREGON_SIGNAL_32B_SENSOR_CODE
                    dwSensorID = CUInt(&H1A2D << 16) Or wSensorID

                    Exit Select
                    ' Simule une THN132
                Case VirtualProbeType.TEMP_SENSOR
                    iSensorType = DOMO_EVENT_ACTION_OREGON_SIGNAL_32B_SENSOR_CODE
                    dwSensorID = CUInt(&H1 << 16) Or wSensorID

                    Exit Select
                    ' Simule un pluviometre
                Case VirtualProbeType.WATER_SENSOR
                    iSensorType = DOMO_EVENT_ACTION_OREGON_SIGNAL_32B_SENSOR_CODE
                    dwSensorID = CUInt(&H2A19 << 16) Or wSensorID
                    Exit Select
            End Select


            ZBS.header = ZBS.GetBytesFromString("ZSIG")
            ZBS.command = 11
            ZBS.alphacommand = ZBS.GetBytesFromString("VProbe")
            ZBS.label_base = ZBS.GetBytesFromString("")

            ZBS.serial = 0

            ZBS.param1 = 6
            ZBS.param2 = dwSensorID
            ZBS.param3 = dwValue1 Or (dwValue2 << 16) Or (dwLowBat << 24)
            ZBS.param4 = CUInt(iSensorType)

            SendToZibase(sZibaseName, ZBS)
        End Sub

        Public Sub SetPlatform(dwPlatform As UInt32, dwPasswordIn As UInt32, dwPasswordOut As UInt32)
            SetPlatform("", dwPlatform, dwPasswordIn, dwPasswordOut)
        End Sub

        Public Sub SetPlatform(sZibaseName As String, dwPlatform As UInt32, dwPasswordIn As UInt32, dwPasswordOut As UInt32)
            Dim ZBS = New ZBClass()

            ZBS.header = ZBS.GetBytesFromString("ZSIG")
            ZBS.command = 11
            ZBS.alphacommand = ZBS.GetBytesFromString("SetPlatform")
            ZBS.label_base = ZBS.GetBytesFromString("")

            ZBS.serial = 0

            ZBS.param1 = 7
            ZBS.param2 = dwPasswordIn
            ZBS.param3 = dwPasswordOut
            ZBS.param4 = dwPlatform

            SendToZibase(sZibaseName, ZBS)
        End Sub


        ' Permet d'associer un token à une zibase. Ce token sera ensuite utilisé pour récupérer des données depuis la plateforme zodianet (liste des scénarios par exemple)
        Public Sub SetZibaseToken(sZibaseName As String, sToken As String)
            If (m_ZibaseList.Keys.Contains(sZibaseName)) Then
                Dim zb = New ZibaseInfo()

                zb = m_ZibaseList(sZibaseName)
                zb.sToken = sToken
                m_ZibaseList(sZibaseName) = zb
            End If
        End Sub

        Public Function GetScenarioList(sZibaseName As String) As String
            Dim functionReturnValue As String = Nothing
            If (m_ZibaseList.Keys.Contains(sZibaseName)) Then
                Dim zb = New ZibaseInfo()

                zb = m_ZibaseList(sZibaseName)

                If (zb.sToken Is Nothing) Then
                    functionReturnValue = "Token must be defined"
                Else
                    ' On charge la liste des scènarios depuis la plateforme zodianet
                    Dim xDoc = New XmlDocument()

                    xDoc.Load("http://www.zibase.net/m/get_xml.php?device=" & sZibaseName & "&token=" & zb.sToken)

                    Dim scenario As XmlNodeList = xDoc.GetElementsByTagName("m")
                    Dim sSceList As String = Nothing

                    sSceList = ""

                    If True Then
                        Dim s As String = Nothing
                        Dim sce_name As String = Nothing

                        For Each node As XmlNode In scenario
                            s = node.Attributes.GetNamedItem("id").Value
                            sce_name = node.ChildNodes.Item(0).InnerText

                            If (Not String.IsNullOrEmpty(sSceList)) Then
                                sSceList = sSceList & "|"
                            End If
                            sSceList = sSceList & sce_name & ";" & s
                        Next

                        functionReturnValue = sSceList
                    End If
                End If
            Else
                functionReturnValue = "Zibase not found"
            End If
            Return functionReturnValue
        End Function

        Public Function GetDevicesList(sZibaseName As String) As String
            Dim functionReturnValue As String = Nothing
            If (m_ZibaseList.Keys.Contains(sZibaseName)) Then
                Dim zb = New ZibaseInfo()

                zb = m_ZibaseList(sZibaseName)

                If (zb.sToken Is Nothing) Then
                    functionReturnValue = "Token must be defined"
                Else
                    ' On charge la liste des scènarios depuis la plateforme zodianet
                    Dim xDoc = New XmlDocument()

                    xDoc.Load("http://www.zibase.net/m/get_xml.php?device=" & sZibaseName & "&token=" & zb.sToken)

                    Dim sensors As XmlNodeList = xDoc.GetElementsByTagName("e")
                    Dim sSensorsList As String = Nothing

                    sSensorsList = ""
                    Dim stype As String = Nothing
                    Dim sid As String = Nothing
                    Dim sce_name As String = Nothing

                    For Each node As XmlNode In sensors
                        stype = node.Attributes.GetNamedItem("t").Value
                        sid = node.Attributes.GetNamedItem("c").Value
                        sce_name = node.ChildNodes.Item(0).InnerText

                        If (Not String.IsNullOrEmpty(sSensorsList)) Then
                            sSensorsList = sSensorsList & "|"
                        End If
                        sSensorsList = sSensorsList & sce_name & ";" & stype & ";" & sid
                    Next

                    functionReturnValue = sSensorsList
                End If
            Else
                functionReturnValue = "Zibase not found"
            End If
            Return functionReturnValue
        End Function
    End Class

    Public Class ZBClass

        Public header As Byte() = New Byte(4) {}
        Public command As UInt16
        Public alphacommand As Byte() = New Byte(8) {}
        Public serial As UInt32
        Public sid As UInt32
        Public label_base As Byte() = New Byte(16) {}
        Public my_ip As UInt32
        Public my_port As UInt32
        Public reserved1 As UInt32
        Public reserved2 As UInt32
        Public param1 As UInt32
        Public param2 As UInt32
        Public param3 As UInt32
        Public param4 As UInt32
        Public my_count As UInt16
        Public your_count As UInt16

        Public command_text As Byte() = New Byte(96) {}

        Public Sub SetData(data As Byte())
            If (data Is Nothing) Then
                Return
            End If
            If (data.Length < 70) Then
                Return
            End If

            Array.Copy(data, 0, header, 0, 4)

            Array.Reverse(data, 4, 2)
            command = BitConverter.ToUInt16(data, 4)

            Array.Copy(data, 6, alphacommand, 0, 8)

            'Dim s As String = System.Text.Encoding.Default.GetString(alphacommand)

            Array.Reverse(data, 14, 4)
            serial = BitConverter.ToUInt32(data, 14)

            Array.Reverse(data, 18, 4)
            sid = BitConverter.ToUInt32(data, 18)

            Array.Copy(data, 22, label_base, 0, 16)

            Array.Reverse(data, 38, 4)
            my_ip = BitConverter.ToUInt32(data, 38)

            Array.Reverse(data, 42, 4)
            reserved1 = BitConverter.ToUInt32(data, 42)

            Array.Reverse(data, 46, 4)
            reserved2 = BitConverter.ToUInt32(data, 46)

            Array.Reverse(data, 50, 4)
            param1 = BitConverter.ToUInt32(data, 50)

            Array.Reverse(data, 54, 4)
            param2 = BitConverter.ToUInt32(data, 54)

            Array.Reverse(data, 58, 4)
            param3 = BitConverter.ToUInt32(data, 58)

            Array.Reverse(data, 62, 4)
            param4 = BitConverter.ToUInt32(data, 62)

            Array.Reverse(data, 66, 2)
            my_count = BitConverter.ToUInt16(data, 66)

            Array.Reverse(data, 68, 2)
            your_count = BitConverter.ToUInt16(data, 68)

            ' Sur un paquet de type étendu, on extrait en plus la commande
            If (data.Length = 166) Then
                ' s = System.Text.Encoding.Default.GetString(command_text)
                Array.Copy(data, 70, command_text, 0, 96)
            End If
        End Sub

        Private Sub CopyBytes(ByRef Data As Byte(), val As UInt32, ByRef iCur As Integer)
            Dim temp As Byte() = Nothing
            temp = BitConverter.GetBytes(val)
            Array.Reverse(temp)
            Array.Copy(temp, 0, Data, iCur, 4)
            iCur = iCur + 4
        End Sub

        Private Sub CopyBytes(ByRef Data As Byte(), val As UInt16, ByRef iCur As Integer)
            Dim temp As Byte() = Nothing
            temp = BitConverter.GetBytes(val)
            Array.Reverse(temp)
            Array.Copy(temp, 0, Data, iCur, 2)
            iCur = iCur + 2
        End Sub

        Private Sub CopyBytes(ByRef Data As Byte(), val As Byte(), iSize As Integer, ByRef iCur As Integer)
            Dim i As Integer = 0

            For i = 0 To iSize - 1
                If (i < val.Length) Then
                    Data(iCur) = val(i)
                Else
                    Data(iCur) = 0
                End If
                iCur = iCur + 1
            Next
        End Sub


        Public Function GetBytes() As Byte()
            Dim data As Byte() = New Byte(69) {}
            Dim iCur As Integer = 0

            CopyBytes(data, header, 4, iCur)
            CopyBytes(data, command, iCur)
            CopyBytes(data, alphacommand, 8, iCur)
            CopyBytes(data, serial, iCur)
            CopyBytes(data, sid, iCur)
            CopyBytes(data, label_base, 16, iCur)

            CopyBytes(data, my_ip, iCur)
            CopyBytes(data, reserved1, iCur)
            CopyBytes(data, reserved2, iCur)

            CopyBytes(data, param1, iCur)
            CopyBytes(data, param2, iCur)
            CopyBytes(data, param3, iCur)
            CopyBytes(data, param4, iCur)

            CopyBytes(data, my_count, iCur)
            CopyBytes(data, your_count, iCur)

            If (command_text(0) <> 0) Then
                Array.Resize(data, 166)
                CopyBytes(data, command_text, 96, iCur)
            End If

            Return data
        End Function

        Public Function GetBytesFromString(sSrc As String) As Byte()
            Dim arr As Byte() = New Byte(sSrc.Length) {}
            Dim i As Integer = 0

            For i = 0 To sSrc.Length - 1
                arr(i) = DirectCast(Convert.ChangeType(sSrc(i), TypeCode.[Byte]), [Byte])
            Next

            Return arr

        End Function

        Public Sub SetServerPort(dwPort As UInt32)
            my_port = dwPort
        End Sub

        Public Function InitZapi(sZibaseIP As String, sLocalIP As String) As String
            Dim ZBS As New ZBClass()
            Dim IpAddr As IPAddress = Nothing
            Dim sZibaseName As String = ""

            ZBS.header = GetBytesFromString("ZSIG")
            ZBS.command = 13
            ZBS.alphacommand = GetBytesFromString("ZapiInit")
            ZBS.label_base = GetBytesFromString("")

            ZBS.serial = 0

            ' If (sAddr = "10.40.1.255") Then
            '       IpAdd(i) = IPAddress.Parse("192.168.1.16")
            ' End If

            IpAddr = IPAddress.Parse(sLocalIP)

            Dim temp As Byte() = IpAddr.GetAddressBytes()
            Array.Reverse(temp)
            ZBS.param1 = BitConverter.ToUInt32(temp, 0)
            ZBS.param2 = my_port
            ZBS.param3 = 0
            ZBS.param4 = 0

            Dim data As Byte() = Nothing
            UDPDataTransmit(ZBS.GetBytes(), data, sZibaseIP, 49999)

            'Detection d'une nouvelle zibase
            If (data IsNot Nothing) Then
                If data.Length >= 70 Then
                    Dim ZBSrcv As New ZBClass()
                    ZBSrcv.SetData(data)
                    sZibaseName = System.Text.Encoding.[Default].GetString(ZBSrcv.label_base)
                    Dim iPos As Integer = Strings.InStr(sZibaseName, Strings.Chr(0))
                    If iPos > 0 Then
                        sZibaseName = sZibaseName.Substring(iPos - 1)
                        ' Strings.Left(sZibaseName, iPos - 1);
                    End If
                End If
            End If

            Return sZibaseName
        End Function

        ' Cette fonction permet de parcourrir les différents réseaux incluant le PC et de transmettre un ordre d'activation de l'API Zapi
        Public Sub BrowseForZibase()
            Dim i As Integer = 0

            ' On liste les adresses IP du PC
            Dim ipEnter As IPHostEntry = Dns.GetHostEntry(Dns.GetHostName())
            Dim IpAdd As IPAddress() = ipEnter.AddressList


            For i = 0 To IpAdd.GetUpperBound(0)

                If IpAdd(i).AddressFamily = AddressFamily.InterNetwork Then
                    Dim sLocalIP As String = ((IpAdd(i).GetAddressBytes()(0) + "." + IpAdd(i).GetAddressBytes()(1) & ".") + IpAdd(i).GetAddressBytes()(2) & ".") + IpAdd(i).GetAddressBytes()(3)
                    Dim sBroadcastIP As String = (IpAdd(i).GetAddressBytes()(0) + "." + IpAdd(i).GetAddressBytes()(1) & ".") + IpAdd(i).GetAddressBytes()(2) & ".255"

                    InitZapi(sBroadcastIP, sLocalIP)
                End If
            Next
        End Sub

        Public Function UDPDataTransmit(sBuff As Byte(), ByRef rBuff As Byte(), IP As String, Port As Integer) As Integer
            'Returns # bytes received

            Dim retstat As Integer = 0
            Dim Sck As System.Net.Sockets.Socket = Nothing
            Dim Due As DateTime = Nothing
            Try
                Sck = New Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                Sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000)
                Sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 2000)

                Dim Encrp As New IPEndPoint(IPAddress.Parse(IP), Port)

                retstat = Sck.SendTo(sBuff, 0, sBuff.Length, SocketFlags.None, Encrp)

                If retstat > 0 Then
                    Due = DateTime.Now.AddMilliseconds(2000)
                    '10 second time-out

                    While Sck.Available = 0 AndAlso DateTime.Now < Due
                    End While

                    If Sck.Available = 0 Then
                        'timed-out
                        retstat = -3
                        Return retstat
                    End If

                    rBuff = New Byte(Sck.Available - 1) {}


                    retstat = Sck.Receive(rBuff, 0, Sck.Available, SocketFlags.None)
                Else
                    ' fail on send
                    retstat = -1
                End If
            Catch generatedExceptionName As Exception
                'General Exception received--add code here to analyze the exception. A messagebox would be one idea.
                retstat = -2
            Finally
                'Always close the socket when done.
                Sck.Close()
            End Try
            Return retstat
        End Function



    End Class

    ' loic.ploumen
    ' created only for VB to C# conversion purpose
    ' TODO : remove it :)
    Public NotInheritable Class Strings
        Private Sub New()
        End Sub
        Public Shared Function Len(s As [String]) As Integer
            Return s.Length
        End Function

        Public Shared Function InStr(s As [String], s1 As [String]) As Integer
            Return s.IndexOf(s1)
        End Function

        Public Shared Function InStr(s As [String], c As Char) As Integer
            Return s.IndexOf(c)
        End Function

        Public Shared Function Chr(v As Integer) As Char
            Return ChrW(v)
        End Function

        Public Shared Function Asc(c As Char) As Integer
            Return Val(c)
            '    Return c
        End Function

        Public Shared Function Mid(s As String, a As Integer, b As Integer) As String
            Dim temp As String = s.Substring(a - 1, b)
            Return temp
        End Function

        Public Shared Function Mid(s As String, a As Integer) As String
            Dim temp As String = s.Substring(a - 1)
            Return temp
        End Function
    End Class
End Namespace