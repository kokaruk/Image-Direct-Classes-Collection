Imports System.Reflection

<Serializable()>
Public Class ImgdirCustomer

    ' Store received 
    Private _aCustomer As String
    Private _prismCustName As String = String.Empty
    Public Property prismCustName As String
        Get
            Return _prismCustName
        End Get
        Private Set(value As String)
            _prismCustName = value
        End Set
    End Property
    Private _prinergyCustName As String = String.Empty
    Public Property prinergyCustName As String
        Get
            Return _prinergyCustName
        End Get
        Private Set(value As String)
            _prinergyCustName = value
        End Set
    End Property
    Private _imgDirDates As ImgDirDates
    Friend ReadOnly Property imgDirDates As ImgDirDates
        Get
            Return _imgDirDates
        End Get
    End Property

    Private _cycleType As String
    Public ReadOnly Property cycleType As String
        Get
            Return _cycleType
        End Get
    End Property


    'constructor
    Public Sub New(ByVal myCustomer As String)

        If String.IsNullOrEmpty(myCustomer) Then Throw New Exception("Customer is Empty String")
        _aCustomer = myCustomer
        ' check if _aCustomer querry return PrinergyCustomer 
        Try
            prinergyCustName = ImgDirCustDB.getPrinergyCustomer(_aCustomer)
            If Not String.IsNullOrEmpty(prinergyCustName) Then prismCustName = _aCustomer
        Catch ex As Exception
        End Try
        'If Previos querry return nothing or error, try querry for prism cust
        If String.IsNullOrEmpty(prinergyCustName) Then
            Try
                prismCustName = ImgDirCustDB.getPrismCustomer(_aCustomer)
                If Not String.IsNullOrEmpty(prismCustName) Then prinergyCustName = _aCustomer
            Catch ex As Exception
            End Try
        End If
        If prismCustName = String.Empty OrElse prinergyCustName = String.Empty Then Throw New Exception(String.Format("Failed to Initiate Customer Class. The string ""{0}"" returns no results from database search", myCustomer))
        _cycleType = ImgDirCustDB.getReleaseDescription(myCustomer)
        _imgDirDates = createImgDirDatesInstance()
    End Sub

    Private Function createImgDirDatesInstance() As ImgDirDates
        Dim imgDirDatesName As String = String.Concat("IMGDIR2.ImgDirDates", Char.ToUpper(_cycleType(0)) + _cycleType.Substring(1))
        Dim imgDirType As Type = Type.GetType(imgDirDatesName)
        Return Activator.CreateInstance(imgDirType, {Me})
    End Function

End Class
