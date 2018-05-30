<Serializable()>
Public Class ImgdirOrderItem

    Public Property customer As ImgdirCustomer

    'time stamp of initiation, needed for PDF
    Private _myTimeInit As Date = Date.Now

    Private _idOfSequence As Integer = Nothing
    Public Property idOfSequence As Integer
        Get
            Return _idOfSequence
        End Get
        Private Set(value As Integer)
            _idOfSequence = value
        End Set
    End Property

    Private _itemQty As Integer = Nothing
    Public Property itemQty As Integer
        Get
            Return _itemQty
        End Get
        Private Set(value As Integer)
            _itemQty = value
        End Set
    End Property

    Private _countQty As Integer = Nothing
    Public Property coutQty As Integer
        Get
            If Nothing = _countQty Then
                _countQty = itemQty * UOM
            End If
            Return _countQty
        End Get
        Private Set(value As Integer)
            _countQty = value
        End Set
    End Property

    Private _UOM As Integer = Nothing
    Public ReadOnly Property UOM As Integer
        Get
            If Nothing = _UOM Then
                _UOM = getUOM()
            End If
            Return _UOM
        End Get
    End Property

    Private _stockCode As String = String.Empty
    Public Property stockCode As String
        Get
            Return _stockCode
        End Get
        Private Set(value As String)
            _stockCode = value
        End Set
    End Property

    Private _itemDesc As String = String.Empty
    Public Property itemDesc As String
        Get
            Return _itemDesc
        End Get
        Private Set(value As String)
            _itemDesc = value
        End Set
    End Property

    'is part part of prinergy job name, needs to have order number to be legit
    Private _itemJobName As String = String.Empty
    Public Property itemJobName As String
        Get
            If String.IsNullOrEmpty(_itemJobName) Then
                _itemJobName = String.Format("{0} {1}", stockCode, idOfSequence.ToString())
            End If
            Return _itemJobName
        End Get
        Private Set(value As String)
            _itemJobName = value
        End Set
    End Property

    Private _attachmentFiles As New List(Of String)()
    Public Property attachmentFiles As List(Of String)
        Get
            Return _attachmentFiles
        End Get
        Private Set(value As List(Of String))
            _attachmentFiles = value
        End Set
    End Property

    ' id of item in the database
    Private _itemId As String = String.Empty
    Public Property itemId As String
        Get
            Return _itemId
        End Get
        Private Set(value As String)
            _itemId = value
        End Set
    End Property

    Private _size As ItemSize = Nothing
    Public Property size As ItemSize
        Get
            Return _size
        End Get
        Private Set(value As ItemSize)
            _size = value
        End Set
    End Property

    Private _processName As String = String.Empty
    Public Property processName As String
        Get
            If String.IsNullOrEmpty(_processName) Then
                _processName = getProcess()
            End If
            Return _processName
        End Get
        Private Set(value As String)
            _processName = value
        End Set
    End Property

    Private _productGroupId As String = String.Empty
    Public Property productGroupId As String
        Get
            If String.IsNullOrEmpty(_productGroupId) Then
                _productGroupId = getProductGroupID()
            End If
            Return _productGroupId
        End Get
        Private Set(value As String)
            _productGroupId = value
        End Set
    End Property

    Public Sub New(ByRef customer As ImgdirCustomer, _
                    ByVal idOfSequence As Byte, _
                    ByVal itemQty As Integer, _
                    ByVal codeStock As String, _
                    ByVal itemDesc As String, _
                    ByVal attachmentFiles As List(Of String) _
                    )
        Me.idOfSequence = idOfSequence
        Me.itemQty = itemQty
        Me.stockCode = codeStock
        Me.itemDesc = itemDesc
        Me.customer = customer
        Me.attachmentFiles = attachmentFiles
        size = ImgDirOrderItemDB.getSize(Me.stockCode)
        Dim i As Nullable(Of ItemSize) = size
        If Not i.HasValue Then Throw New Exception("Failed to initiate ""Size"" property for {0}")
    End Sub

    Private Function getUOM() As Integer
        Return ImgDirOrderItemDB.getUOM(stockCode)
    End Function

    Private Function getProcess() As String
        Return ImgDirOrderItemDB.getPress(stockCode)
    End Function

    Public Sub createNewItemRecordInDb(ByVal orderNum As String)
        itemId = ImgDirOrderItemDB.createNewItemRecordInDb(Me.orderJobName(orderNum), customer.prismCustName)
    End Sub

    Public Sub updateItemRecordInDb(ByVal orderNum As String, ByVal actionString As String)
        ImgDirOrderItemDB.updateItemRecordInDb(itemId, String.Format("{0}: {1} {2}", actionString, orderNum, itemJobName))
    End Sub

    Public Function orderJobName(ByVal orderNum As String) As String
        Return String.Format("{0} {1}", orderNum, itemJobName)
    End Function

    Public Function getProductGroupID() As String
        Return ImgDirOrderItemDB.getProductGroupId(stockCode)
    End Function

    Public Sub addPdfLabelForAttachments(ByVal orderNum As String, ByVal shipTo As ShipTo, ByVal fileFolder As String)
        For Each attachmentFile In Me.attachmentFiles
            Me.addPdfLabelForAnAttachment(orderNum, shipTo, fileFolder, attachmentFile)
        Next
    End Sub

    Private Sub addPdfLabelForAnAttachment(ByVal orderNum As String, ByVal shipTo As ShipTo, ByVal fileFolder As String, ByVal fileName As String)

        ' -- create date of order variable

        Dim dateOrder As String = _myTimeInit.ToString("d", CultureInfo.CreateSpecificCulture("en-AU"))

        Dim fullFilePath As String = fileFolder + fileName
        ' open PDF document
        Using document As PdfDocument = PdfReader.Open(fullFilePath)

            document.Info.Title = String.Format("Prinergy PDF Label. Copyright PMG {0}", _myTimeInit.Year.ToString())

            ' Insert an empty page
            Dim page As PdfPage = document.InsertPage(0)
            page.Width = Me.size.width.toPoints()
            page.Height = Me.size.height.toPoints()
            ' Construct label's delivery text
            Dim addressForDelivery As String = String.Join(ControlChars.NewLine, shipTo.delivery.ToArray())
            ' Instantiate the font
            Dim font As New XFont("Times New Roman", 8, XFontStyle.Regular)
            Dim messageLabel As String
            ' create string without 'HAT' (^) symbol for pdf label
            Dim orderDescriptionInfo() As String = Split(Me.itemDesc, ")", 2)
            Dim itemDesc As String = String.Empty
            Dim cardDet As String = String.Empty
            If orderDescriptionInfo.Length > 1 Then
                cardDet = Right(orderDescriptionInfo(0), _
                                           orderDescriptionInfo(0).Length - 1)
                Dim pattern As String = "(\s*\^)+"
                cardDet = Regex.Replace(cardDet, pattern, ", ").Trim(New [Char]() {","c, " "c})

                itemDesc = Trim(orderDescriptionInfo(1)) _
                         + Chr(32) _
                         + cardDet
            Else
                itemDesc = Me.itemDesc
            End If

            ' check if label needed for label printer or labels imposition
            If (processName.Contains("IND7") OrElse processName.Contains("IND3")) Then
                messageLabel = _
                "Order N: " & orderNum & Chr(10) & Chr(10) & _
                "Date of Order: " & dateOrder & Chr(10) & Chr(10) & _
                "Delivery: " & shipTo.contact & Chr(10) & Chr(10) & _
                addressForDelivery & Chr(10) & _
                "Description: " & itemDesc & Chr(10) & _
                "Qty: " & coutQty.ToString()

            Else
                'need label for label press
                'change label size to 80x85 mm for label printer
                Dim lblPriterLbl As New ItemSize With {.width = New Size(80), .height = New Size(85)}
                page.Width = lblPriterLbl.width.toPoints()
                page.Height = lblPriterLbl.height.toPoints()

                font = New XFont("Times New Roman", 10, XFontStyle.Regular)

                messageLabel = _
                "Order N: " & orderNum & Chr(10) & Chr(10) & _
                "Date of Order: " & Chr(10) & dateOrder & Chr(10) & Chr(10) & _
                "Delivery: " & Chr(10) & shipTo.contact & Chr(10) & Chr(10) & _
                addressForDelivery & Chr(10) & _
                "Description: " & Chr(10) & itemDesc & Chr(10) & Chr(10) & _
                "Qty: " & coutQty.ToString()
            End If

            ' Get an XGraphics object for drawing
            Using gfx As XGraphics = XGraphics.FromPdfPage(page)

                Dim textFormatter As XTextFormatter = New XTextFormatter(gfx)
                textFormatter.Alignment = XParagraphAlignment.Left

                textFormatter.DrawString(messageLabel, font, XBrushes.Black, New XRect(30, 10, page.Width.Point - 60, page.Height.Point - 10), XStringFormats.TopLeft)

                ' BAR CODE
                Dim BarCode39 As New PdfSharp.Drawing.BarCodes.Code3of9Standard()
                BarCode39.TextLocation = New PdfSharp.Drawing.BarCodes.TextLocation()
                BarCode39.Text = orderNum + "$I"

                'value of code to draw on page
                BarCode39.StartChar = Convert.ToChar("*")
                BarCode39.EndChar = Convert.ToChar("*")
                BarCode39.Direction = PdfSharp.Drawing.BarCodes.CodeDirection.TopToBottom
                Dim fontBARCODE As New XFont("Arial", 14, XFontStyle.Regular)
                'make barcode max up to 70 mm
                Dim barcodeSize As New XSize(If(page.Height.Millimeter >= 70, Convert.ToDouble(70 * 2.83464567), page.Height.Point * 0.9), Convert.ToDouble(16))
                BarCode39.Size = barcodeSize
                gfx.DrawBarCode(BarCode39, XBrushes.Black, fontBARCODE, New XPoint(Convert.ToDouble(20), Convert.ToDouble(10)))
                gfx.RotateTransform(90)
                ' Save the document...

                document.Save(fullFilePath)

            End Using

        End Using

    End Sub

End Class
