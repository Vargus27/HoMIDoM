﻿'------------------------------------------------------------------------------
' <auto-generated>
'     Ce code a été généré par un outil.
'     Version du runtime :4.0.30319.296
'
'     Les modifications apportées à ce fichier peuvent provoquer un comportement incorrect et seront perdues si
'     le code est régénéré.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict On
Option Explicit On

Imports System

Namespace My.Resources
    
    'Cette classe a été générée automatiquement par la classe StronglyTypedResourceBuilder
    'à l'aide d'un outil, tel que ResGen ou Visual Studio.
    'Pour ajouter ou supprimer un membre, modifiez votre fichier .ResX, puis réexécutez ResGen
    'avec l'option /str ou régénérez votre projet VS.
    '''<summary>
    '''  Une classe de ressource fortement typée destinée, entre autres, à la consultation des chaînes localisées.
    '''</summary>
    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0"),  _
     Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
     Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute(),  _
     Global.Microsoft.VisualBasic.HideModuleNameAttribute()>  _
    Friend Module Resources
        
        Private resourceMan As Global.System.Resources.ResourceManager
        
        Private resourceCulture As Global.System.Globalization.CultureInfo
        
        '''<summary>
        '''  Retourne l'instance ResourceManager mise en cache utilisée par cette classe.
        '''</summary>
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
        Friend ReadOnly Property ResourceManager() As Global.System.Resources.ResourceManager
            Get
                If Object.ReferenceEquals(resourceMan, Nothing) Then
                    Dim temp As Global.System.Resources.ResourceManager = New Global.System.Resources.ResourceManager("HoMIGuI.Resources", GetType(Resources).Assembly)
                    resourceMan = temp
                End If
                Return resourceMan
            End Get
        End Property
        
        '''<summary>
        '''  Remplace la propriété CurrentUICulture du thread actuel pour toutes
        '''  les recherches de ressources à l'aide de cette classe de ressource fortement typée.
        '''</summary>
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
        Friend Property Culture() As Global.System.Globalization.CultureInfo
            Get
                Return resourceCulture
            End Get
            Set
                resourceCulture = value
            End Set
        End Property
        
        Friend ReadOnly Property delete() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("delete", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        Friend ReadOnly Property dossier() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("dossier", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        Friend ReadOnly Property Driver_32() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("Driver_32", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        Friend ReadOnly Property edit() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("edit", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        Friend ReadOnly Property help() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("help", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        Friend ReadOnly Property Homidom_logo_128() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("Homidom_logo_128", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        Friend ReadOnly Property play() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("play", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        Friend ReadOnly Property reload() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("reload", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        Friend ReadOnly Property restart() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("restart", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        Friend ReadOnly Property settings() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("settings", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
        
        Friend ReadOnly Property stopped() As System.Drawing.Bitmap
            Get
                Dim obj As Object = ResourceManager.GetObject("stopped", resourceCulture)
                Return CType(obj,System.Drawing.Bitmap)
            End Get
        End Property
    End Module
End Namespace
