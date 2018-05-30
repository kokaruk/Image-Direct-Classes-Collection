Option Explicit On
Option Strict On

<Serializable()>
Public Class ImgdirOrderCollection
    Private preCompReleaseModifier As Integer = 0
    Private _name As String = String.Empty
    Public Property name As String
        Get
            If String.IsNullOrEmpty(_name) Then
                _name = String.Format("{0}-{1}", customer.prinergyCustName, customer.imgDirDates.makeCollectionPrefix())
            End If
            Return _name
        End Get
        Private Set(value As String)
            _name = value
        End Set
    End Property

    Private _customer As ImgdirCustomer
    Public Property customer As ImgdirCustomer
        Get
            Return _customer
        End Get
        Private Set(value As ImgdirCustomer)
            _customer = value
        End Set
    End Property

    Private Property dateCreated As Date

    Private _startPeriodDateTime As Date
    Public Property startPeriodDateTime As Date
        Get
            Return _startPeriodDateTime
        End Get
        Private Set(value As Date)
            _startPeriodDateTime = value
        End Set
    End Property
    Private _stopPeriodDateTime As Date

    Public Property stopPeriodDateTime As Date
        Get
            Return _stopPeriodDateTime
        End Get
        Set(value As Date)
            _stopPeriodDateTime = value
        End Set
    End Property

    Public Property orders As New List(Of ImgdirOrder)
    Private _impositionGroups As New List(Of ImpositionGroup)
    Public ReadOnly Property impositionGroups As List(Of ImpositionGroup)
        Get
            If Me.orders.Count > 0 AndAlso (_impositionGroups Is Nothing OrElse _impositionGroups.Count = 0) Then
                _impositionGroups = Me.getImpositionGroups()
            End If
            Return _impositionGroups
        End Get
    End Property

    Public Sub New(ByVal customer As ImgdirCustomer)
        Me.customer = customer
        Me.dateCreated = Date.Now
        startPeriodDateTime = customer.imgDirDates.startPeriodDateTime
        stopPeriodDateTime = customer.imgDirDates.stopPeriodDateTime
    End Sub

    'method required for email report
    Public Function getOrderCountInPeriodForACustomer() As Integer
        Return ImgdirOrderCollectionDB.dbOrdersCount(customer.prinergyCustName, startPeriodDateTime, stopPeriodDateTime)
    End Function
    'generate excell method
    Public Sub writeExcellSpreadsheet(ByVal pathToXls As String)
        Dim book As New Workbook()
        ' Specify which Sheet should be opened and the size of window by default
        book.ExcelWorkbook.ActiveSheetIndex = 0
        book.ExcelWorkbook.WindowTopX = 0
        book.ExcelWorkbook.WindowTopY = 0
        book.ExcelWorkbook.WindowHeight = 25000
        book.ExcelWorkbook.WindowWidth = 20000
        ' Some optional properties of the Document
        book.Properties.Author = "PrinergyAutomations"
        book.Properties.Title = Me.name
        book.Properties.Created = DateTime.Now

        ' Add few styles to the Workbook
        Dim style As WorksheetStyle = book.Styles.Add("MyHeader")
        style.Font.FontName = "Tahoma"
        style.Font.Size = 8
        style.Font.Bold = True
        style.Alignment.Horizontal = StyleHorizontalAlignment.Center
        style.Font.Color = "White"
        style.Interior.Color = "#0D49FF" 'BLUE
        style.Interior.Pattern = StyleInteriorPattern.DiagCross

        style = book.Styles.Add("MyHeader2")
        style.Font.FontName = "Tahoma"
        style.Font.Size = 10
        style.Font.Bold = True
        style.Alignment.Horizontal = StyleHorizontalAlignment.Center
        style.Font.Color = "White"
        style.Interior.Color = "#FF0100" ' RED
        style.Interior.Pattern = StyleInteriorPattern.DiagCross

        style = book.Styles.Add("Wrapper")
        style.Alignment.WrapText = True
        style.Alignment.Vertical = StyleVerticalAlignment.Top
        style.Font.Size = 8
        style.Borders.Add(StylePosition.Bottom, LineStyleOption.Continuous, 1)

        style = book.Styles.Add("WrapperNoBorder")
        style.Alignment.WrapText = True
        style.Alignment.Vertical = StyleVerticalAlignment.Top
        style.Font.Size = 8

        ' crete new sheet
        Dim sheet As Worksheet = book.Worksheets.Add(Me.name)

        For Each impositionGroup In Me.impositionGroups
            Dim row As WorksheetRow = sheet.Table.Rows.Add()
            Dim cellStock As WorksheetCell = row.Cells.Add(impositionGroup.prodStock)
            cellStock.MergeAcross = 4
            cellStock.StyleID = "MyHeader2"
            Dim pressOfGroup As WorksheetCell = row.Cells.Add(impositionGroup.press)
            pressOfGroup.StyleID = "MyHeader2"

            row = sheet.Table.Rows.Add()
            Dim columnHeaders(,) As Object

            Select Case Me.customer.prinergyCustName
                Case "ANZ AUTOMATIONS"
                    columnHeaders = New Object(,) { _
                                                    {"#", 35}, _
                                                    {"Cstmr", 40}, _
                                                    {"BSB", 50}, _
                                                    {"Part", 70}, _
                                                    {"Qty", 30}, _
                                                    {"Descr", 70}, _
                                                    {"Delivery", 100}, _
                                                    {"Card Info", 100} _
                                                  }
                Case Else
                    columnHeaders = New Object(,) { _
                                                    {"#", 35}, _
                                                    {"Cstmr", 40}, _
                                                    {"Part", 70}, _
                                                    {"Qty", 30}, _
                                                    {"Descr", 70}, _
                                                    {"Delivery", 110}, _
                                                    {"Card Info", 100}}

            End Select


            For i As Integer = 0 To columnHeaders.GetUpperBound(0)
                sheet.Table.Columns.Add(New WorksheetColumn(CInt(columnHeaders(i, 1))))
                row.Cells.Add(New WorksheetCell(CStr(columnHeaders(i, 0)), "MyHeader"))
            Next

            Dim fileNameCounter As Integer = 0

            'For Each filename As String In inputFiles
            For Each myOrderJobName In impositionGroup.orders

                fileNameCounter += 1
                Dim myOrderJobNameString As String = myOrderJobName
                Dim myOrder As ImgdirOrder = Me.orders.Find(Function(c) c.orderItems(0).orderJobName(c.orderNo.ToString) = myOrderJobNameString)

                Dim itemDesc As String
                Dim cardDet As String
                'split XML data from item description 
                Dim orderDescriptionInfo() As String = Split(myOrder.orderItems(0).itemDesc, ")", 2)
                If orderDescriptionInfo.Length > 1 Then
                    itemDesc = Trim(orderDescriptionInfo(1))
                   
                    cardDet = Right(orderDescriptionInfo(0), _
                                          orderDescriptionInfo(0).Length - 1)
                    Dim pattern As String = "(\s*\^)+"
                    cardDet = Regex.Replace(cardDet, pattern, ", ").Trim(New [Char]() {","c, " "c})
                Else
                    itemDesc = myOrder.orderItems(0).itemDesc
                    cardDet = String.Empty
                End If
                Dim shipTo As String = myOrder.shipTo.contact + ", " + String.Join(", ", myOrder.shipTo.delivery.ToArray)
                row = sheet.Table.Rows.Add()
                Dim orderProperties() As String
                Select Case Me.customer.prinergyCustName
                    Case "ANZ AUTOMATIONS"
                        orderProperties = New String() { _
                                              myOrder.orderNo.ToString(), _
                                              myOrder.customer.prismCustName, _
                                              myOrder.costCentre, _
                                              myOrder.orderItems(0).stockCode, _
                                              myOrder.orderItems(0).coutQty.ToString(), _
                                              itemDesc, _
                                              shipTo, _
                                              cardDet _
                                              }
                    Case Else
                        orderProperties = New String() { _
                                              myOrder.orderNo.ToString(), _
                                              myOrder.customer.prismCustName, _
                                              myOrder.orderItems(0).stockCode, _
                                              myOrder.orderItems(0).coutQty.ToString(), _
                                              itemDesc, _
                                              shipTo, _
                                              cardDet _
                                              }
                End Select
                For Each orderProperty As String In orderProperties
                    Dim orderInfo As WorksheetCell = row.Cells.Add(orderProperty)
                    If fileNameCounter <> impositionGroup.orders.Count Then
                        orderInfo.StyleID = "Wrapper"
                    Else
                        orderInfo.StyleID = "WrapperNoBorder"
                    End If
                Next
            Next
        Next
        book.Save(pathToXls)
    End Sub

    ' method to generate XmL list for product groups
    Public Sub writeXmlForImpositonGroups(ByVal pathToXml As String)
        If Me.impositionGroups.Count = 0 Then Exit Sub
        If File.Exists(pathToXml) Then File.Delete(pathToXml)
        Using writer As New XmlTextWriter(pathToXml, System.Text.Encoding.UTF8) _
                                        With {.Formatting = Formatting.Indented, _
                                             .QuoteChar = Chr(39)}
            writer.WriteStartDocument()
            ' Write the root element.
            writer.WriteStartElement("Impositions")
            For Each impositionGroup In Me.impositionGroups
                ' Write the title element.
                writer.WriteStartElement("Group")
                '<Group id='55x90_21up_5DC' Qty='21' Type='BC' Stock='BIDVEST 340GSM SPLENDORGEL DS' Press='M-IND7600' EmptySpace='3' UOM='250'>


                Dim myAttributes() As myXmlAttribute = _
                                { _
                                New myXmlAttribute With {.attributeName = "id", .attributeValue = impositionGroup.method}, _
                                New myXmlAttribute With {.attributeName = "Qty", .attributeValue = impositionGroup.sqty.ToString()}, _
                                New myXmlAttribute With {.attributeName = "Type", .attributeValue = impositionGroup.prodType}, _
                                New myXmlAttribute With {.attributeName = "Stock", .attributeValue = impositionGroup.prodStock}, _
                                New myXmlAttribute With {.attributeName = "Press", .attributeValue = impositionGroup.press}, _
                                New myXmlAttribute With {.attributeName = "EmptySpace", .attributeValue = impositionGroup.emptySpace.ToString()}, _
                                New myXmlAttribute With {.attributeName = "UOM", .attributeValue = impositionGroup.UOM.ToString()}
                                }
                imgdirXMLCreator.writeMyXmLattribute(writer, myAttributes)
                For Each myOrderJobName In impositionGroup.orders
                    writer.WriteElementString("Input", myOrderJobName + ".pdf")
                Next
                writer.WriteEndElement()
            Next
            ' Write the close tag for the root element.
            writer.WriteEndElement()
            writer.WriteEndDocument()

        End Using
    End Sub

    Private Function getImpositionGroups() As List(Of ImpositionGroup)
        Dim myImpositionGroups As New List(Of ImpositionGroup)
        For Each impositionGroupId In Me.getListOfimpositionGroupID()
            Dim impositionGroupTable As DataTable = ImgdirOrderCollectionDB.getImpositionGroupTable(impositionGroupId)
            If impositionGroupTable.Rows.Count <> 1 Then Throw New Exception("imposition table request either has no data or returns multiple rows")
            Dim myImpositionGroup As New ImpositionGroup With { _
                             .groupId = impositionGroupId, _
                             .method = CStr(impositionGroupTable.Rows(0).Item("method")), _
                             .templatePath = CStr(impositionGroupTable.Rows(0).Item("templatePath")), _
                             .sqty = CInt(impositionGroupTable.Rows(0).Item("sqty")), _
                             .prodType = CStr(impositionGroupTable.Rows(0).Item("type")), _
                             .prodStock = CStr(impositionGroupTable.Rows(0).Item("name")), _
                             .press = CStr(impositionGroupTable.Rows(0).Item("process")), _
                             .jdfDest = CStr(impositionGroupTable.Rows(0).Item("jdfDest")), _
                             .emptySpace = CInt(impositionGroupTable.Rows(0).Item("emptyspace")), _
                             .UOM = CInt(impositionGroupTable.Rows(0).Item("UOM")), _
                             .orders = New List(Of String) _
                             }
            For Each order In Me.orders
                If myImpositionGroup.groupId = order.orderItems(0).productGroupId Then
                    myImpositionGroup.orders.Add(order.orderItems(0).orderJobName(order.orderNo.ToString()))
                End If
            Next
            myImpositionGroups.Add(myImpositionGroup)

        Next
        Return myImpositionGroups
    End Function

    Private Function getListOfimpositionGroupID() As List(Of String)
        If Me.orders.Count < 1 Then Throw New Exception("No OrderItems Found in Collection")
        Dim mYlistOfimpositionGroupID As New List(Of String)
        For Each order In Me.orders
            If Not mYlistOfimpositionGroupID.Contains(order.orderItems(0).productGroupId) Then
                mYlistOfimpositionGroupID.Add(order.orderItems(0).productGroupId)
            End If
        Next
        mYlistOfimpositionGroupID.Sort()
        Return mYlistOfimpositionGroupID
    End Function

    Public Shared Sub orderCollectionToBinaryStream(ByVal binaryFilePath As String, ByVal myCollection As ImgdirOrderCollection)
        If File.Exists(binaryFilePath) Then File.Delete(binaryFilePath)
        Using fileStream As Stream = New FileStream(binaryFilePath, FileMode.Create, _
            FileAccess.Write, FileShare.None)
            Dim serilizerFormatter As New BinaryFormatter()
            serilizerFormatter.Serialize(fileStream, myCollection)
        End Using
    End Sub

    Public Shared Function deSerializer(ByVal binaryFilePath As String) As ImgdirOrderCollection
        Dim deserialize As ImgdirOrderCollection

        Using FileStream As Stream = New FileStream(binaryFilePath, _
                                                    FileMode.Open, _
                                                    FileAccess.Read, FileShare.Read)
            Dim formatter As IFormatter = New BinaryFormatter
            deserialize = DirectCast(formatter.Deserialize(FileStream), ImgdirOrderCollection)
        End Using

        Return deserialize
    End Function

    Public Function getimpositionProcess(ByVal impositionGroupRepetitionNumber As Integer) As String
        Return Me.impositionGroups(impositionGroupRepetitionNumber - 1).templatePath + _
               Me.impositionGroups(impositionGroupRepetitionNumber - 1).method
    End Function

    Public Function getTextFileDestination(ByVal impositionGroupRepetitionNumber As Integer) As String
        Return Me.impositionGroups(impositionGroupRepetitionNumber - 1).jdfDest
    End Function

    Public Function getEmailDestination(ByVal impositionGroupRepetitionNumber As Integer) As String
        Return ImgdirOrderCollectionDB.getEmailDestination( _
                                        Me.impositionGroups(impositionGroupRepetitionNumber - 1).groupId _
                                                        )
    End Function

End Class
