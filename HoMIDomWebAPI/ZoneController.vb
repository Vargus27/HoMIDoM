﻿
Imports System.Web.Http
Imports HoMIDom
Imports HoMIDom.HoMIDom

Public Class ZoneController
    Inherits BaseHoMIdomController

    Public Function [Get]() As List(Of Zone)
        Return HoMIDomAPI.CurrentServer.GetAllZones(Me.ServerKey)
    End Function

    Public Function [Get](id As String) As Zone
        Return HoMIDomAPI.CurrentServer.ReturnZoneByID(Me.ServerKey, id)
    End Function


    <HttpGet()>
    Public Function ExecuteCommand(id As String, command As String) As Boolean
        HoMIDomAPI.CurrentServer.ExecuteDeviceCommand(Me.ServerKey, id, New DeviceAction() With {.Nom = command})
        Return True
    End Function

End Class