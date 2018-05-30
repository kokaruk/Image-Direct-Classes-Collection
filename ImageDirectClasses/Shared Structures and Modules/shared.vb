<Serializable()>
Public Structure ReleaseTimeInDB
    Dim DayOfWeek As Integer
    Dim Time As Date
End Structure
<Serializable()>
Public Class Size
    Private Const toPointsConversion As Double = 2.83464567
    Private _value As Double
    Public Property value As Double
        Get
            Return _value
        End Get
        Private Set(value As Double)
            _value = value
        End Set
    End Property
    Public Function toPoints() As Double
        Return value * toPointsConversion
    End Function
    Public Sub New(ByVal mYsize As Integer)
        value = mYsize
    End Sub
End Class
<Serializable()>
Public Structure ItemSize
    Dim width As Size
    Dim height As Size
End Structure
<Serializable()>
Public Structure ShipTo
    Dim contact As String
    Dim delivery As List(Of String)
End Structure
<Serializable()>
Public Structure MonthlyPrintWeekDays
    Dim deliveryDay As String
    Dim printDay As String
End Structure
<Serializable()>
Public Class ImpositionGroup
    Friend Property groupId As String
    Friend Property templatePath As String
    Friend Property method As String
    Friend Property sqty As Integer
    Friend Property prodType As String
    Friend Property prodStock As String
    Friend Property press As String
    Friend Property jdfDest As String
    Friend Property emptySpace As Integer
    Friend Property UOM As Integer
    Friend Property orders As List(Of String)
End Class
<Serializable()>
Friend Structure myXmlAttribute
    Dim attributeName As String
    Dim attributeValue As String
End Structure