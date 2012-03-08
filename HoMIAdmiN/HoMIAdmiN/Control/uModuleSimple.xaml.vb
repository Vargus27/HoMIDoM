﻿Imports HoMIDom.HoMIDom.Device
Imports HoMIDom.HoMIDom.Api
Imports System.IO

Class uModuleSimple
    Public Event CloseMe(ByVal MyObject As Object)

    Public Sub New()
        ' Cet appel est requis par le Concepteur Windows Form.
        InitializeComponent()
        Try
            'Ajoute les choix des modules
            CbType.Items.Add("Associer un interrupteur/detecteur à un appareil/lampe/volet")
            CbType.Items.Add("Associer des interrupteurs/detecteurs à un appareil/lampe/volet")
            CbType.Items.Add("Associer un interrupteur/detecteur à des appareils/lampes/volets")
            CbType.Items.Add("Associer des interrupteurs/detecteurs à des appareils/lampes/volets")
            CbType.Items.Add("Programmer une action sur un composant")

            'Ajout des devices
            For Each Device In myService.GetAllDevices(IdSrv)
                CbEmetteur.Items.Add(Device.Name)
                CbRecepteur.Items.Add(Device.Name)

                Dim stk As New StackPanel
                stk.Orientation = Orientation.Horizontal
                Dim x As New CheckBox
                x.Content = Device.Name
                x.ToolTip = Device.Description
                x.IsChecked = False
                stk.Children.Add(x)
                LbEmetteurMulti.Items.Add(stk)

                Dim stk2 As New StackPanel
                stk2.Orientation = Orientation.Horizontal
                Dim y As New CheckBox
                y.Content = Device.Name
                y.ToolTip = Device.Description
                y.IsChecked = False
                stk2.Children.Add(y)
                LbRecepteurMulti.Items.Add(stk2)
            Next
        Catch Ex As Exception
            MessageBox.Show("Erreur: UmoduleSimple New: " & Ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


    Private Sub BtnAnnuler_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BtnAnnuler.Click
        RaiseEvent CloseMe(Me)
    End Sub

    Private Sub BtnAjouter_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BtnAjouter.Click

        RaiseEvent CloseMe(Me)
    End Sub

    Private Sub BtnHelp_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BtnHelp.Click
        MessageBox.Show(BtnHelp.ToolTip, "Aide", MessageBoxButton.OK, MessageBoxImage.Question)
    End Sub

    Private Sub CbType_SelectionChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs) Handles CbType.SelectionChanged
        Select Case CbType.SelectedValue
            Case "Associer un interrupteur/detecteur à un appareil/lampe/volet"
                StkCompEmet.Visibility = Windows.Visibility.Visible
                StkCompRecep.Visibility = Windows.Visibility.Visible
                StkMulti.Visibility = Windows.Visibility.Collapsed
                StkCompMultiEmet.Visibility = Windows.Visibility.Collapsed
                StkCompMultiRecep.Visibility = Windows.Visibility.Collapsed
                StkOrdre.Visibility = Windows.Visibility.Collapsed
            Case "Associer des interrupteurs/detecteurs à un appareil/lampe/volet"
                StkCompEmet.Visibility = Windows.Visibility.Collapsed
                StkCompRecep.Visibility = Windows.Visibility.Visible
                StkMulti.Visibility = Windows.Visibility.Visible
                StkCompMultiEmet.Visibility = Windows.Visibility.Visible
                StkCompMultiRecep.Visibility = Windows.Visibility.Collapsed
                StkOrdre.Visibility = Windows.Visibility.Collapsed
            Case "Associer un interrupteur/detecteur à des appareils/lampes/volets"
                StkCompEmet.Visibility = Windows.Visibility.Visible
                StkCompRecep.Visibility = Windows.Visibility.Collapsed
                StkMulti.Visibility = Windows.Visibility.Visible
                StkCompMultiEmet.Visibility = Windows.Visibility.Collapsed
                StkCompMultiRecep.Visibility = Windows.Visibility.Visible
                StkOrdre.Visibility = Windows.Visibility.Collapsed
            Case "Associer des interrupteurs/detecteurs à des appareils/lampes/volets"
                StkCompEmet.Visibility = Windows.Visibility.Collapsed
                StkCompRecep.Visibility = Windows.Visibility.Collapsed
                StkMulti.Visibility = Windows.Visibility.Visible
                StkCompMultiEmet.Visibility = Windows.Visibility.Visible
                StkCompMultiRecep.Visibility = Windows.Visibility.Visible
                StkOrdre.Visibility = Windows.Visibility.Collapsed
            Case "Programmer une action sur un composant"
                StkCompEmet.Visibility = Windows.Visibility.Collapsed
                StkCompRecep.Visibility = Windows.Visibility.Visible
                StkMulti.Visibility = Windows.Visibility.Collapsed
                StkCompMultiEmet.Visibility = Windows.Visibility.Collapsed
                StkCompMultiRecep.Visibility = Windows.Visibility.Collapsed
                StkOrdre.Visibility = Windows.Visibility.Visible
        End Select
    End Sub
End Class