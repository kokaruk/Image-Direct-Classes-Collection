Friend Module imgdirXMLCreator
    Friend Sub writeMyXmLattribute(ByRef writer As XmlTextWriter, ByVal myAttributes() As myXmlAttribute)
        For Each myAttribute In myAttributes
            writer.WriteAttributeString(myAttribute.attributeName, myAttribute.attributeValue)
        Next
    End Sub
End Module
