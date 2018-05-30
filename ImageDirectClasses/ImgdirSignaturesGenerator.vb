Option Explicit On
Option Strict On

Public Class ImgdirSignaturesGenerator
    Private _orderCollection As ImgdirOrderCollection
    Public Property orderCollection As ImgdirOrderCollection
        Get
            Return _orderCollection
        End Get
        Private Set(value As ImgdirOrderCollection)
            _orderCollection = value
        End Set
    End Property
    Private Property impositionGroup As New ImpositionGroup
    Private Property impositionGroupOrders As New List(Of ImgdirOrder)
    Private Property oversetQtyOrders As New List(Of ImgdirOrder)
    Private Property signatures As New List(Of Signature)

    Public Sub New(ByVal orderCollectionBinaryPath As String)
        Me.orderCollection = ImgdirOrderCollection.deSerializer(orderCollectionBinaryPath)
    End Sub
    Public Sub makeSignatures(ByVal impositionGroupRepetitionNumber As Integer, ByVal pathToJobsFolder As String)
        'retrieve imposition group from collection in orderCollection
        impositionGroup = orderCollection.impositionGroups(impositionGroupRepetitionNumber - 1)
        'build local copy of orders of type ImgdirOrder in imposition group
        impositionGroupOrders = Me.getOrders()

        ' split orders list in two lists
        ' groupOfOrdersOversetQty where order qty greater than sign spaces 
        oversetQtyOrders = impositionGroupOrders.FindAll(AddressOf itemQtyGreaterThenSigSpaces)
        If oversetQtyOrders.Count > 0 Then
            ' remove overset orders from 'all orders' imposition group and sort new group by qty using
            impositionGroupOrders.RemoveAll(AddressOf itemQtyGreaterThenSigSpaces)
            ' sort orders by Quantity property
            Me.oversetQtyOrders.Sort(New sortOrderByQtyAsc)
            ' recursevly call overset orders method
            Me.processOrders(Me.oversetQtyOrders)
        End If
        If impositionGroupOrders.Count > 0 Then
            impositionGroupOrders.Sort(New sortOrderByQtyAsc)
            processOrders(impositionGroupOrders)
        End If
        Me.writeXmlForImpositonGroups(pathToJobsFolder + _
                                      Me.orderCollection.name + "-" + _
                                      impositionGroupRepetitionNumber.ToString() + _
                                      ".xml")
        Me.updateSpreadsheet(pathToJobsFolder + _
                             Me.orderCollection.name + _
                             ".xls")
    End Sub

    ' method to search for all overset orders with same
    ' order quantity in order list
    Private Sub processOrders(ByRef ordersList As List(Of ImgdirOrder))
        Dim firsItemQty As Integer = ordersList(0).orderItems(0).itemQty
        Dim myPredicateItemQty = Function(s As ImgdirOrder) s.orderItems(0).itemQty = firsItemQty
        ' Get all orders from the list with the same order count
        Dim sameQtyOrders As List(Of ImgdirOrder) = ordersList.FindAll(myPredicateItemQty)
        If sameQtyOrders.Count > 0 Then
            'remove orders with same count from the original passed list
            ordersList.RemoveAll(myPredicateItemQty)
            ' call method to generate signatures with the same qty, populate class' signatures list 
            ' and return not full signatures with same quantity for further processing
            Dim notFullSignatures As List(Of Signature) = Me.generateSameQtySignatures(sameQtyOrders)
            If notFullSignatures.Count > 0 Then
                For Each notFullSignature In notFullSignatures
                    Me.stepSignatureAndProcess(notFullSignature)
                Next
            End If
        End If
        ' If there are still orders left in the orders list, self call recuresevly method processOrders 
        If ordersList.Count > 0 Then Me.processOrders(ordersList)
    End Sub

    ' method to generate signatures from list of ImgDirOrder and return not full signatures
    Private Function generateSameQtySignatures(ByRef sameQtyOrders As List(Of ImgdirOrder)) As List(Of Signature)
        Dim myNotFullSignatures As New List(Of Signature)
        Do While sameQtyOrders.Count > 0
            Dim ordersRangeCount As Integer = If(sameQtyOrders.Count < Me.impositionGroup.sqty, sameQtyOrders.Count, Me.impositionGroup.sqty)
            ' instantiate new signature. If signature is full, add to class instace signatures list
            ' if signature is not full add to method signatures list
            Dim thisItereationSignature As New Signature(Me.impositionGroup)
            With thisItereationSignature
                .denominator = sameQtyOrders(0).orderItems(0).itemQty
                .pages = sameQtyOrders.GetRange(0, ordersRangeCount)
            End With
            If thisItereationSignature.pages.Count = Me.impositionGroup.sqty Then
                Me.signatures.Add(thisItereationSignature)
            Else
                myNotFullSignatures.Add(thisItereationSignature)
            End If

            sameQtyOrders.RemoveRange(0, ordersRangeCount)
        Loop
        Return myNotFullSignatures
    End Function

    Private Sub stepSignatureAndProcess(ByVal notFullSignature As Signature)
        ' see if I can find orders with same denominator to fill the gaps
        ' the following method operates on notFullSignature byref and returns no value
        Me.searchForElementsToFillGaps(notFullSignature)
        ' see if I can step orders on the imposition
        ' set denominator copy, to check if it has changed after the procedure
        Dim originalDenominator As Double = notFullSignature.denominator
        Dim notFullSignaturePagesCopy As New List(Of ImgdirOrder)
        notFullSignaturePagesCopy.AddRange(notFullSignature.pages)
        Dim ordersStepperCounter As Integer = 1  ' stepper count

        Do While notFullSignature.emptySpaces - notFullSignaturePagesCopy.Count >= 0
            ordersStepperCounter += 1
            notFullSignature.pages.AddRange(notFullSignaturePagesCopy)
            notFullSignature.denominator = originalDenominator / ordersStepperCounter
        Loop

        ' if still signature has gaps, see if can find orders to fill with changed denominator
        If notFullSignature.emptySpaces > 0 AndAlso originalDenominator > notFullSignature.denominator Then Me.searchForElementsToFillGaps(notFullSignature)

        notFullSignature.pages.Sort(New sortOrderByOrderNum)
        ' empty space check
        If notFullSignature.emptySpaces <= Me.impositionGroup.emptySpace Then
            Me.signatures.Add(notFullSignature)
        Else
            If hasMultiplePages(notFullSignature) Then
                Me.spiltSignatureAndProcess(notFullSignature)
            Else
                Me.reverseSteppingForSingleOrder(notFullSignature)
            End If
        End If
    End Sub

    Private Sub searchForElementsToFillGaps(ByRef notFullSignature As Signature)
        Dim myNotFullSignDenominator As Double = notFullSignature.denominator
        Dim myPredicateItemGreaterInit = _
                    Function(s As ImgdirOrder) s.orderItems(0).itemQty >= myNotFullSignDenominator
        ' see if there are any overset orders or there are orders with common denominator and if not so exit method
        If (Me.oversetQtyOrders.Find(myPredicateItemGreaterInit)) Is Nothing _
                    AndAlso _
                   (Me.impositionGroupOrders.Find(myPredicateItemGreaterInit)) Is Nothing _
                    Then Exit Sub

        ' see if can find orders with same denominator
        If notFullSignature.emptySpaces > 0 Then

            For emptySpaceTicker As Integer = notFullSignature.emptySpaces To 1 Step -1
                ' ==========   ****    For emptySpaceTicker As Integer = 1 To notFullSignature.emptySpaces
                If notFullSignature.emptySpaces <= 0 Then Exit Sub
                'searching items with QTY = ticker * denominator
                Dim notFullSigDenominTimesEmptySpaceTicker As Double = myNotFullSignDenominator * emptySpaceTicker
                'See if there are possible candidates
                Dim myPredicateItemGreater = _
                    Function(s As ImgdirOrder) s.orderItems(0).itemQty >= notFullSigDenominTimesEmptySpaceTicker
                ' search for candidates in overset orders and remaining orders
                If (Me.oversetQtyOrders.Find(myPredicateItemGreater)) Is Nothing _
                    AndAlso _
                   (Me.impositionGroupOrders.Find(myPredicateItemGreater)) Is Nothing _
                    Then Continue For
                Dim myPredicate_ItemQtyEqualsNotFullSignDenomTimesEmptSpaceTicker = _
                Function(s As ImgdirOrder) s.orderItems(0).itemQty = notFullSigDenominTimesEmptySpaceTicker
                ' Get all orders from orders list where order qty = denominator x EmptSpaceTicker
                Dim candidateOrdersToFit As New List(Of ImgdirOrder)
                ' first find in all overset orders, add all possible orders, then add back all unused to overset list
                candidateOrdersToFit.AddRange(Me.oversetQtyOrders.FindAll(myPredicate_ItemQtyEqualsNotFullSignDenomTimesEmptSpaceTicker))
                If candidateOrdersToFit.Count > 0 Then
                    Me.oversetQtyOrders.RemoveAll(myPredicate_ItemQtyEqualsNotFullSignDenomTimesEmptSpaceTicker)
                    Me.oversetQtyOrders.AddRange( _
                                         Me.fillEmptySpacesOnSignature( _
                                                    notFullSignature, _
                                                    candidateOrdersToFit, _
                                                    emptySpaceTicker) _
                                                  )
                    Me.oversetQtyOrders.Sort(New sortOrderByQtyAsc)
                    candidateOrdersToFit.Clear()
                End If
                candidateOrdersToFit.AddRange(Me.impositionGroupOrders.FindAll(myPredicate_ItemQtyEqualsNotFullSignDenomTimesEmptSpaceTicker))
                If candidateOrdersToFit.Count > 0 Then
                    Me.impositionGroupOrders.RemoveAll(myPredicate_ItemQtyEqualsNotFullSignDenomTimesEmptSpaceTicker)
                    Me.impositionGroupOrders.AddRange( _
                                         Me.fillEmptySpacesOnSignature( _
                                                    notFullSignature, _
                                                    candidateOrdersToFit, _
                                                    emptySpaceTicker) _
                                                  )
                    Me.impositionGroupOrders.Sort(New sortOrderByQtyAsc)
                    ' no need to clear candidateOrdersToFit as it is the end of iteration
                End If
            Next
        End If
    End Sub

    Private Function fillEmptySpacesOnSignature(ByRef notFullSignature As Signature, _
                                           ByRef possibleOrdersToFit As List(Of ImgdirOrder), _
                                           ByVal emptySpaceTicker As Integer) As List(Of ImgdirOrder)
        If notFullSignature.emptySpaces < 1 Then
            possibleOrdersToFit.Clear()
            Return possibleOrdersToFit
        End If

        ' requiredSpaces int for how many orders we can fit
        Dim canTakeordersRange As Integer = CInt(Math.Floor(notFullSignature.emptySpaces / emptySpaceTicker))
        Dim ordersToAdd As New List(Of ImgdirOrder)
        If possibleOrdersToFit.Count > canTakeordersRange Then
            ordersToAdd.AddRange(possibleOrdersToFit.GetRange(0, canTakeordersRange))
            possibleOrdersToFit.RemoveRange(0, canTakeordersRange)
        Else
            ordersToAdd.AddRange(possibleOrdersToFit)
            possibleOrdersToFit.Clear()
        End If
        For i As Integer = 1 To emptySpaceTicker
            notFullSignature.pages.AddRange(ordersToAdd)
        Next

        'remove orders with same count from the original passed list
        Return possibleOrdersToFit
    End Function

    ' method to split signature. Backwards remove iteration, starting from the largest if can't step orders and still too many empty spaces
    Private Sub spiltSignatureAndProcess(ByVal notFullSignature As Signature)

        notFullSignature.pages.Sort(New sortOrderByQtyAsc)
        ' Split oorders list on Signature in two. 
        ' 1. Total orders minus rounded (spaces on signature / 2) 
        ' 2. The Rest
        Dim notFullSignatureExtra As New Signature(Me.impositionGroup) With { _
                                          .denominator = notFullSignature.denominator, _
                                          .pages = New List(Of ImgdirOrder)
                                       }

        Do Until impositionGroup.sqty - _
                 notFullSignature.pages.Count * Math.Floor(impositionGroup.sqty / notFullSignature.pages.Count) <= impositionGroup.emptySpace
            '    Dim tempInteger As Integer = Convert.ToInt32(Me.impositionGroup.sqty - notFullSignature.pages.Count * Math.Floor(Me.impositionGroup.sqty / notFullSignature.pages.Count))
            If notFullSignature.pages.Count <= 0 Then Exit Do
            Dim greatestOrderByQtyInSignature As Integer = notFullSignature.pages(notFullSignature.pages.Count - 1).orderNo
            Dim myPredicateOrderNumber = Function(s As ImgdirOrder) s.orderNo = greatestOrderByQtyInSignature
            notFullSignatureExtra.pages.AddRange(notFullSignature.pages.FindAll(myPredicateOrderNumber))
            notFullSignature.pages.RemoveAll(myPredicateOrderNumber)
        Loop



        If notFullSignature.pages.Count > 0 Then Me.stepSignatureAndProcess(notFullSignature)

        ' check if all orders of the same qty
        Dim firsItemQty As Integer = notFullSignatureExtra.pages(0).orderItems(0).itemQty

        Dim allOrdersSameQty As Boolean = False
        For Each imgdirOrder In notFullSignatureExtra.pages
            '  Dim myPredicateItemQty = Function(s As ImgdirOrder) s.orderItems(0).itemQty = firsItemQty
            allOrdersSameQty = (imgdirOrder.orderItems(0).itemQty = firsItemQty)
        Next

        If allOrdersSameQty Then
            Dim sameQtyOrders As New List(Of ImgdirOrder)
            ' get all orders
            For Each imgdirOrder In notFullSignatureExtra.pages
                If Not sameQtyOrders.Contains(imgdirOrder) Then sameQtyOrders.Add(imgdirOrder)
            Next
            processOrders(sameQtyOrders)
        Else
            Me.stepSignatureAndProcess(notFullSignatureExtra)
        End If


    End Sub

    ' method to check if signature has multiple orders
    Private Function hasMultiplePages(ByVal notFullSignature As Signature) As Boolean
        Dim firstPageOrderNumber As Integer = notFullSignature.pages(notFullSignature.pages.Count - 1).orderNo
        Return notFullSignature.pages.Count <> notFullSignature.pages.FindAll(Function(s As ImgdirOrder) s.orderNo = firstPageOrderNumber).Count
    End Function
    ' method to bring multiple instances of the same order on signature to one
    Private Sub reverseSteppingForSingleOrder(ByRef notFullSignature As Signature)
        Dim pagesStepped As Integer = notFullSignature.pages.Count
        notFullSignature.pages.RemoveRange(1, pagesStepped - 1)
        notFullSignature.denominator *= pagesStepped
        Me.stepSignatureAndProcess(notFullSignature)
    End Sub

    ' method to generate XmL list for product groups
    Private Sub writeXmlForImpositonGroups(ByVal pathToXml As String)
        If Me.signatures.Count = 0 Then Exit Sub
        If File.Exists(pathToXml) Then File.Delete(pathToXml)
        Using writer As New XmlTextWriter(pathToXml, System.Text.Encoding.UTF8) _
                                        With {.Formatting = Formatting.Indented, _
                                             .QuoteChar = Chr(39)}
            ' Write the start of document element.
            writer.WriteStartDocument()
            ' Write ' Write the start of document element. element.
            writer.WriteStartElement("Impositions")

            Dim signaturesIdCounter As Integer = 1
            For Each signature In Me.signatures
                ' Write the title element.
                writer.WriteStartElement("Signature")
                Dim myAttributes() As myXmlAttribute = _
                                { _
                                New myXmlAttribute With {.attributeName = "ID",
                                                         .attributeValue = signaturesIdCounter.ToString()}, _
                                New myXmlAttribute With {.attributeName = "Runs",
                                                         .attributeValue = signature.signaturePrintRun().ToString()}
                                }
                imgdirXMLCreator.writeMyXmLattribute(writer, myAttributes)

                Dim pagesCounter As Integer = 1
                For Each page In signature.pages
                    ' Write Page element.
                    writer.WriteStartElement("Page")
                    writer.WriteAttributeString("Position", pagesCounter.ToString())
                    writer.WriteValue(page.orderItems(0).orderJobName(page.orderNo.ToString()) + ".pdf")
                    writer.WriteEndElement()
                    pagesCounter += 1
                Next

                writer.WriteEndElement()
                signaturesIdCounter += 1
            Next
            ' Write the close tag for the root element.
            writer.WriteEndElement()
            writer.WriteEndDocument()
        End Using
    End Sub
    ' Method to update Excell Spreadsheet
    Private Sub updateSpreadsheet(ByVal pathToSpreadsheet As String)
        Dim book As New Workbook()
        book.Load(pathToSpreadsheet)
        If book.Styles.IndexOf("Signature") < 0 Then
            Dim style As WorksheetStyle = book.Styles.Add("Signature")
            style.Font.FontName = "Tahoma"
            style.Font.Size = 8
            style.Font.Bold = True
            style.Alignment.Horizontal = StyleHorizontalAlignment.Center
            style.Alignment.Vertical = StyleVerticalAlignment.Center
            style.Alignment.VerticalText = True
            style.Font.Color = "White"
            style.Interior.Color = "#FEFF00" 'Yellow
            style.Interior.Pattern = StyleInteriorPattern.Gray75
        End If
        Dim columnHeaders(,) As Object

        Select Case Me.orderCollection.customer.prinergyCustName
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
        Dim sheet As Worksheet
        If book.Worksheets.Count > 1 Then
            sheet = book.Worksheets(1)
        Else
            sheet = book.Worksheets.Add("_signatures")
            For i As Integer = 0 To columnHeaders.GetUpperBound(0)
                sheet.Table.Columns.Add()
            Next
            ' last coloumn with signature number
            sheet.Table.Columns.Add(New WorksheetColumn(10))
        End If

        'add production group name row
        Dim row As WorksheetRow = sheet.Table.Rows.Add()
        Dim cellStock As WorksheetCell = row.Cells.Add(Me.impositionGroup.prodStock)
        cellStock.MergeAcross = 4
        cellStock.StyleID = "MyHeader2"
        Dim qtyStockCell As WorksheetCell = row.Cells.Add()
        qtyStockCell.MergeAcross = 3
        qtyStockCell.StyleID = "MyHeader2"
        row = sheet.Table.Rows.Add()
        Dim qtyStock As Integer
        For i As Integer = 0 To columnHeaders.GetUpperBound(0)
            sheet.Table.Columns.Add(New WorksheetColumn(CInt(columnHeaders(i, 1))))
            row.Cells.Add(New WorksheetCell(CStr(columnHeaders(i, 0)), "MyHeader"))
        Next

        For Each signature In Me.signatures
            qtyStock += signature.signaturePrintRun()
            Dim signatureRow As New WorksheetRow
            Dim uniquePages As New List(Of ImgdirOrder)

            For Each Page In signature.pages
                If Not uniquePages.Contains(Page) Then uniquePages.Add(Page)
            Next

            For Each page In uniquePages
                Dim orderRow As WorksheetRow = sheet.Table.Rows.Add()
                If uniquePages.IndexOf(page) = 0 Then signatureRow = orderRow

                Dim itemDesc As String
                Dim cardDest As String
                'split XML data from item description 
                Dim orderDescriptionInfo() As String = Split(page.orderItems(0).itemDesc, ")", 2)

                If orderDescriptionInfo.Length > 1 Then
                    itemDesc = Trim(orderDescriptionInfo(1))
                    cardDest = Right(orderDescriptionInfo(0), _
                                           orderDescriptionInfo(0).Length - 1)
                    Dim pattern As String = "(\s*\^)+"
                    cardDest = Regex.Replace(cardDest, pattern, ", ").Trim(New [Char]() {","c, " "c})
                Else
                    itemDesc = page.orderItems(0).itemDesc
                    cardDest = String.Empty
                End If
                Dim shipTo As String = page.shipTo.contact + ", " + String.Join(", ", page.shipTo.delivery.ToArray)
                Dim orderProperties() As String
                Select Case Me.orderCollection.customer.prinergyCustName
                    Case "ANZ AUTOMATIONS"
                        orderProperties = New String() { _
                                              page.orderNo.ToString(), _
                                              page.customer.prismCustName, _
                                              page.costCentre, _
                                              page.orderItems(0).stockCode, _
                                              page.orderItems(0).coutQty.ToString(), _
                                              itemDesc, _
                                              shipTo, _
                                              cardDest _
                                              }
                    Case Else
                        orderProperties = New String() { _
                                              page.orderNo.ToString(), _
                                              page.customer.prismCustName, _
                                              page.orderItems(0).stockCode, _
                                              page.orderItems(0).coutQty.ToString(), _
                                              itemDesc, _
                                              shipTo, _
                                              cardDest _
                                              }
                End Select

                For Each orderProperty As String In orderProperties
                    Dim orderCell As WorksheetCell = orderRow.Cells.Add(orderProperty)
                    If Me.signatures.IndexOf(signature) = Me.signatures.Count - 1 _
                    AndAlso uniquePages.IndexOf(page) = uniquePages.Count - 1 Then
                        orderCell.StyleID = "WrapperNoBorder"
                    Else
                        orderCell.StyleID = "Wrapper"
                    End If
                Next

            Next

            Dim signatureCell As WorksheetCell = signatureRow.Cells.Add((Me.signatures.IndexOf(signature) + 1).ToString())
            signatureCell.StyleID = "Signature"
            signatureCell.MergeDown = uniquePages.Count - 1
            ' add separator row if not the last signature
            If Me.signatures.IndexOf(signature) <> Me.signatures.Count - 1 Then
                Dim separatoRow As WorksheetRow = sheet.Table.Rows.Add()
                separatoRow.Height = 5
            End If
        Next
        'update qte Cell with total stock sheets quantity
        qtyStockCell.Data.Text = String.Format("Total Stock Sheets: {0}", qtyStock.ToString())
        book.Save(pathToSpreadsheet)
    End Sub
    ' private method to convert  from list(of string) of orders name
    ' to a list of ImgdirOrder objects 
    Private Function getOrders() As List(Of ImgdirOrder)
        Dim impositionGroupOrders As New List(Of ImgdirOrder)
        For Each myOrderJobName In Me.impositionGroup.orders
            Dim myOrderJobNameString As String = myOrderJobName
            impositionGroupOrders.Add(Me.orderCollection.orders.Find(Function(c) c.orderItems(0).orderJobName(c.orderNo.ToString) = myOrderJobNameString))
        Next
        Return impositionGroupOrders
    End Function
    ' Predicate Method to Search for not full signatures
    ' *** this predicate no longer used
    Private Function signatureNotFull(ByVal signature As Signature) As Boolean
        Return signature.pages.Count < Me.impositionGroup.sqty
    End Function
    ' Predicate method to search for impositiongroup
    Private Function itemQtyGreaterThenSigSpaces(ByVal order As ImgdirOrder) As Boolean
        Return order.orderItems(0).itemQty > Me.impositionGroup.sqty
    End Function
    'ICOMPARER method for sorting ORDERS by QTY, Descending
    Private Class sortOrderByQtyDesc
        Implements IComparer(Of ImgdirOrder)
        Function Compare(ByVal x As ImgdirOrder, ByVal y As ImgdirOrder) As Integer _
            Implements IComparer(Of ImgdirOrder).Compare
            Dim compareQty As Integer = x.orderItems(0).itemQty.CompareTo(y.orderItems(0).itemQty)
            Return -compareQty
        End Function
    End Class
    'ICOMPARER method for sorting ORDERS by QTY, Ascending
    Private Class sortOrderByQtyAsc
        Implements IComparer(Of ImgdirOrder)
        Function Compare(ByVal x As ImgdirOrder, ByVal y As ImgdirOrder) As Integer _
            Implements IComparer(Of ImgdirOrder).Compare
            Dim compareQty As Integer = x.orderItems(0).itemQty.CompareTo(y.orderItems(0).itemQty)
            Return compareQty
        End Function
    End Class
    'ICOMPARER method for sorting ORDERS by order number
    Private Class sortOrderByOrderNum
        Implements IComparer(Of ImgdirOrder)
        Function Compare(ByVal x As ImgdirOrder, ByVal y As ImgdirOrder) As Integer _
            Implements IComparer(Of ImgdirOrder).Compare
            Dim compareOrderNum As Integer = x.orderNo.CompareTo(y.orderNo)
            Return compareOrderNum
        End Function
    End Class

    Private Class Signature
        ' Signature RUN is a dynamic number, should be calculated on request of output process
        ' no need to set member of struct for this
        Private Property impositionGroup As ImpositionGroup
        Public Property denominator As Double
        Public Property pages As List(Of ImgdirOrder)
        Public ReadOnly Property emptySpaces As Integer
            Get
                Return Me.impositionGroup.sqty - Me.pages.Count
            End Get
        End Property

        Public Sub New(ByRef impositionGroup As ImpositionGroup)
            Me.impositionGroup = impositionGroup
        End Sub
        Public Function signaturePrintRun() As Integer
            Const noExtrSteps As Integer = 1
            Const threeSteps As Integer = 3
            Const fourSteps As Integer = 4
            Const fiveSteps As Integer = 5
            Const sevenSteps As Integer = 7

            Dim extraStepping As Integer = noExtrSteps ' the variable to hold extra stepping for each card

            Select Case Me.impositionGroup.method
                Case "88x54_7up_x3_5DC"
                    extraStepping = threeSteps
                Case "90x55_21up_5DC_4KIND"
                    extraStepping = fourSteps
                Case "90x55_21up_5DC_5KIND"
                    extraStepping = fiveSteps
                Case "90x55_3UPx7_5DC_10mmSlit"
                    extraStepping = sevenSteps
                Case "90x55_3UPx7_5DC_13mmSlit"
                    extraStepping = sevenSteps
                Case Else
                    extraStepping = noExtrSteps
            End Select

            Return CInt(Math.Ceiling(Me.denominator / extraStepping * Me.impositionGroup.UOM))
        End Function
    End Class

End Class