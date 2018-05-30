Option Explicit On
Option Strict On

Imports Npgsql

Friend Module ImgdirPostgresDataLayer

    Friend Function insertSelectTransaction(ByVal sql As String, ByVal parameterNames() As String, ByVal parameterVals() As String) As String
        Using connection As NpgsqlConnection = GetDbConnection()
            'Create a new transaction
            Using transaction As NpgsqlTransaction = connection.BeginTransaction
                Try
                    Using command As New NpgsqlCommand(sql, connection, transaction)
                        FillParameters(command, parameterNames, parameterVals)
                        Dim result As Object = command.ExecuteScalar()
                        'No exceptions encountered
                        transaction.Commit()
                        Return CStr(result)
                    End Using
                Catch ex As Exception
                    'Transaction rolled back to the original state
                    transaction.Rollback()
                    Throw
                End Try
            End Using
        End Using
    End Function

    Friend Function updateTransaction(ByVal sql As String, ByVal parameterNames() As String, ByVal parameterVals() As String) As Integer
        Using connection As NpgsqlConnection = GetDbConnection()
            'Create a new transaction
            Using transaction As NpgsqlTransaction = connection.BeginTransaction
                Try
                    Using command As New NpgsqlCommand(sql, connection, transaction)
                        FillParameters(command, parameterNames, parameterVals)
                        Dim rowSaffected = command.ExecuteNonQuery()
                        'No exceptions encountered
                        transaction.Commit()
                        Return rowSaffected
                    End Using
                Catch ex As Exception
                    'Transaction rolled back to the original state
                    transaction.Rollback()
                    Throw
                End Try
            End Using
        End Using
    End Function

    Friend Function GetDataTable(ByVal sql As String, ByVal parameterNames() As String, ByVal parameterVals() As String) As DataTable
        Using connection As NpgsqlConnection = GetDbConnection()
            Using da As New NpgsqlDataAdapter(sql, connection)
                Dim table As New DataTable
                FillParameters(da.SelectCommand, parameterNames, parameterVals)
                da.Fill(table)
                Return table
            End Using
        End Using
    End Function

    Friend Function GetDataTable(ByVal sql As String) As DataTable
        Using connection As NpgsqlConnection = GetDbConnection()
            Using da As New NpgsqlDataAdapter(sql, connection)
                Dim table As New DataTable
                da.Fill(table)
                Return table
            End Using
        End Using
    End Function

    Friend Function SelectScalar(ByVal sql As String, ByVal parameterNames() As String, ByVal parameterVals() As String) As String
        Using connection As NpgsqlConnection = GetDbConnection()
            Using command As New NpgsqlCommand(sql, connection)
                FillParameters(command, parameterNames, parameterVals)
                Return CStr(command.ExecuteScalar)
            End Using
        End Using
    End Function

    Friend Function SelectScalar(ByVal sql As String) As String
        Using connection As NpgsqlConnection = GetDbConnection()
            Using command As New NpgsqlCommand(sql, connection)
                Return CStr(command.ExecuteScalar)
            End Using
        End Using
    End Function

    Friend Function ExecuteNonQuery(ByVal sql As String, ByVal parameterNames() As String, ByVal parameterVals() As String) As Integer
        Using connection As NpgsqlConnection = GetDbConnection()
            Using command As New NpgsqlCommand(sql, connection)
                FillParameters(command, parameterNames, parameterVals)
                Return command.ExecuteNonQuery()
            End Using
        End Using
    End Function

    Private Sub FillParameters(ByVal command As NpgsqlCommand, ByVal parameterNames As String(), ByVal parameterVals As String())
        If parameterNames IsNot Nothing Then
            For i = 0 To parameterNames.Length - 1
                command.Parameters.AddWithValue(parameterNames(i), parameterVals(i))
            Next
        End If
    End Sub

    Private Function GetDbConnection() As NpgsqlConnection
        Dim conString As String = ConfigurationManager.ConnectionStrings("Prinlut").ConnectionString
        ' read connectionstring from ini file
        'Dim conString As String = getConnectionString()
        Dim connection As New NpgsqlConnection(conString)
        connection.Open()
        Return connection
    End Function

End Module