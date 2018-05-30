<Serializable()>
Public Class ImgdirOrder
    'pre-release time modifier
    Private preCompReleaseModifier As Integer = -24
    Private _orderNo As Integer
    Public Property orderNo As Integer
        Get
            Return _orderNo
        End Get
        Private Set(ByVal value As Integer)
            _orderNo = value
        End Set
    End Property
    Private _customer As ImgdirCustomer
    Public Property customer As ImgdirCustomer
        Get
            Return _customer
        End Get
        Private Set(ByVal value As ImgdirCustomer)
            _customer = value
        End Set
    End Property
    Private _costCentre As String
    Public Property costCentre As String
        Get
            Return _costCentre
        End Get
        Private Set(ByVal value As String)
            _costCentre = value
        End Set
    End Property
    Private _shipto As ShipTo
    Public Property shipTo As ShipTo
        Get
            Return _shipto
        End Get
        Private Set(ByVal value As ShipTo)
            _shipto = value
        End Set
    End Property

    Private Property dateReceived As Date
   
    Public Property orderItems As New List(Of ImgdirOrderItem)

    Public Sub New(ByVal orderNo As Integer, _
                   ByVal customer As ImgdirCustomer, _
                   ByVal costCentre As String, _
                   ByVal orderItems As List(Of ImgdirOrderItem), _
                   ByVal contact As String, _
                   ByVal delivery As List(Of String))
        Me.orderNo = orderNo
        Me.customer = customer
        Me.costCentre = costCentre
        Me.dateReceived = Date.Now
        Me.orderItems = orderItems
        Me.shipTo = New ShipTo With {.contact = contact, .delivery = delivery}
    End Sub

    Public Shared Sub orderToBinaryStream(ByVal binaryFilePath As String, ByVal myOrder As ImgdirOrder)
        If File.Exists(binaryFilePath) Then File.Delete(binaryFilePath)
        Using fileStream As Stream = New FileStream(binaryFilePath, FileMode.Create, _
            FileAccess.Write, FileShare.None)
            Dim serilizerFormatter As New BinaryFormatter()
            serilizerFormatter.Serialize(fileStream, myOrder)
        End Using
    End Sub

    Public Shared Function deSerializer(ByVal binaryFilePath As String) As ImgdirOrder
        Dim deserialize As ImgdirOrder
        Using FileStream As Stream = New FileStream(binaryFilePath, _
                                                    FileMode.Open, _
                                                    FileAccess.Read, FileShare.Read)
            Dim formatter As IFormatter = New BinaryFormatter
            deserialize = DirectCast(formatter.Deserialize(FileStream), ImgdirOrder)
        End Using
        Return deserialize
    End Function
End Class
