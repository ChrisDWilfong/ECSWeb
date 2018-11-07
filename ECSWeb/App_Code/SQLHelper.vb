Imports Microsoft.VisualBasic
Imports System.Data.SqlClient
Imports System.Xml
Imports System.Data

Public Class SQLHelper
    Private Shared gdbConnection As String


    Public Shared Function StripString(ByVal strIn As String) As String
        Dim strOut As String = strIn

        strOut = strOut.Replace("SELECT", "")
        strOut = strOut.Replace("DELETE", "")
        strOut = strOut.Replace("UPDATE", "")
        strOut = strOut.Replace("INSERT", "")
        strOut = strOut.Replace("INTO", "")
        strOut = strOut.Replace("FROM", "")
        strOut = strOut.Replace(".JS", "")
        strOut = strOut.Replace("'", "")            ' Added 8/4/2014...

        Return strOut

    End Function


#Region "Servers"

    Public Shared ReadOnly Property SQLConnection() As String
        Get
            If gdbConnection = "" Then
                gdbConnection = System.Configuration.ConfigurationManager.ConnectionStrings("ECSConnectionString").ToString
            End If
            Return gdbConnection
        End Get
    End Property


#End Region

#Region "private utility methods & constructors"

    'Since this class provides only static methods, make the default constructor private to prevent 

    Private Sub New()
    End Sub 'New

    Private Shared Sub AttachParameters(ByVal command As SqlCommand, ByVal commandParameters() As SqlParameter)
        Dim p As SqlParameter
        For Each p In commandParameters
            'check for derived output value with no value assigned
            If p.Direction = ParameterDirection.InputOutput And p.Value Is Nothing Then
                p.Value = Nothing
            End If
            command.Parameters.Add(p)
        Next p
    End Sub 'AttachParameters

    Private Shared Sub AssignParameterValues(ByVal commandParameters() As SqlParameter, ByVal parameterValues() As Object)

        Dim i As Short
        Dim j As Short

        If (commandParameters Is Nothing) And (parameterValues Is Nothing) Then
            'do nothing if we get no data
            Return
        End If

        'value array
        j = commandParameters.Length - 1
        For i = 0 To j
            commandParameters(i).Value = parameterValues(0)(i)
        Next

    End Sub 'AssignParameterValues

    Private Shared Sub PrepareCommand(ByVal command As SqlCommand,
                                      ByVal connection As SqlConnection,
                                      ByVal transaction As SqlTransaction,
                                      ByVal commandType As CommandType,
                                      ByVal commandText As String,
                                      ByVal commandParameters() As SqlParameter)

        'if the provided connection is not open, we will open it
        If connection.State <> ConnectionState.Open Then
            connection.Open()
        End If

        command.CommandTimeout = 120      ' 09/20/2006 changed... (from the default of 30 secs)...

        'associate the connection with the command
        command.Connection = connection

        'set the command text (stored procedure name or SQL statement)
        command.CommandText = commandText

        'if we were provided a transaction, assign it.
        If Not (transaction Is Nothing) Then
            command.Transaction = transaction
        End If

        'set the command type
        command.CommandType = commandType

        'attach the command parameters if they are provided
        If Not (commandParameters Is Nothing) Then
            AttachParameters(command, commandParameters)
        End If

        Return
    End Sub 'PrepareCommand

#End Region

#Region "ExecuteNonQuery"

    Public Overloads Shared Function ExecuteNonQuery(ByVal connectionString As String,
                                                   ByVal commandType As CommandType,
                                                   ByVal commandText As String) As Integer
        'pass through the call providing null for the set of SqlParameters
        Return ExecuteNonQuery(connectionString, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function 'ExecuteNonQuery

    Public Overloads Shared Function ExecuteNonQuery(ByVal connectionString As String,
                                                     ByVal commandType As CommandType,
                                                     ByVal commandText As String,
                                                     ByVal ParamArray commandParameters() As SqlParameter) As Integer
        'create & open a SqlConnection, and dispose of it after we are done.
        Dim cn As New SqlConnection(connectionString)
        Try
            cn.Open()

            'call the overload that takes a connection in place of the connection string
            Return ExecuteNonQuery(cn, commandType, commandText, commandParameters)
        Finally
            cn.Dispose()
        End Try
    End Function 'ExecuteNonQuery

    Public Overloads Shared Function ExecuteNonQuery(ByVal connectionString As String,
                                                     ByVal spName As String,
                                                     ByVal ParamArray parameterValues() As Object) As Integer
        Dim commandParameters As SqlParameter()

        'if we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) And parameterValues.Length > 0 Then
            'pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)

            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)

            'assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues)

            'call the overload that takes an array of SqlParameters
            Return ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName, commandParameters)
            'otherwise we can just call the SP without params
        Else
            Return ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName)
        End If
    End Function 'ExecuteNonQuery

    Public Overloads Shared Function ExecuteNonQuery(ByVal connection As SqlConnection,
                                                     ByVal commandType As CommandType,
                                                     ByVal commandText As String) As Integer
        'pass through the call providing null for the set of SqlParameters
        Return ExecuteNonQuery(connection, commandType, commandText, CType(Nothing, SqlParameter()))

    End Function 'ExecuteNonQuery

    Public Overloads Shared Function ExecuteNonQuery(ByVal connection As SqlConnection,
                                                     ByVal commandType As CommandType,
                                                     ByVal commandText As String,
                                                     ByVal ParamArray commandParameters() As SqlParameter) As Integer

        'create a command and prepare it for execution
        Dim cmd As New SqlCommand
        Dim retval As Integer

        PrepareCommand(cmd, connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters)

        'finally, execute the command.
        retval = cmd.ExecuteNonQuery()

        'detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()

        Return retval

    End Function 'ExecuteNonQuery

    Public Overloads Shared Function ExecuteNonQuery(ByVal connection As SqlConnection,
                                                     ByVal spName As String,
                                                     ByVal ParamArray parameterValues() As Object) As Integer
        Dim commandParameters As SqlParameter()

        'if we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) And parameterValues.Length > 0 Then
            'pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection.ConnectionString, spName)

            'assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues)

            'call the overload that takes an array of SqlParameters
            Return ExecuteNonQuery(connection, CommandType.StoredProcedure, spName, commandParameters)
            'otherwise we can just call the SP without params
        Else
            Return ExecuteNonQuery(connection, CommandType.StoredProcedure, spName)
        End If

    End Function 'ExecuteNonQuery

    Public Overloads Shared Function ExecuteNonQuery(ByVal transaction As SqlTransaction,
                                                     ByVal commandType As CommandType,
                                                     ByVal commandText As String) As Integer
        'pass through the call providing null for the set of SqlParameters
        Return ExecuteNonQuery(transaction, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function 'ExecuteNonQuery

    Public Overloads Shared Function ExecuteNonQuery(ByVal transaction As SqlTransaction,
                                                     ByVal commandType As CommandType,
                                                     ByVal commandText As String,
                                                     ByVal ParamArray commandParameters() As SqlParameter) As Integer
        'create a command and prepare it for execution
        Dim cmd As New SqlCommand
        Dim retval As Integer

        PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters)

        'finally, execute the command.
        retval = cmd.ExecuteNonQuery()

        'detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()

        Return retval

    End Function 'ExecuteNonQuery

    Public Overloads Shared Function ExecuteNonQuery(ByVal transaction As SqlTransaction,
                                                     ByVal spName As String,
                                                     ByVal ParamArray parameterValues() As Object) As Integer
        Dim commandParameters As SqlParameter()

        'if we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) And parameterValues.Length > 0 Then
            'pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection.ConnectionString, spName)

            'assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues)

            'call the overload that takes an array of SqlParameters
            Return ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName, commandParameters)
            'otherwise we can just call the SP without params
        Else
            Return ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName)
        End If
    End Function 'ExecuteNonQuery


#End Region

#Region "ExecuteDataset"

    Public Overloads Shared Function ExecuteDataset(ByVal connectionString As String,
                                                    ByVal commandType As CommandType,
                                                    ByVal commandText As String) As DataSet
        'pass through the call providing null for the set of SqlParameters
        Return ExecuteDataset(connectionString, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function 'ExecuteDataset

    Public Overloads Shared Function ExecuteDataset(ByVal connectionString As String,
                                                    ByVal commandType As CommandType,
                                                    ByVal commandText As String,
                                                    ByVal ParamArray commandParameters() As SqlParameter) As DataSet
        'create & open a SqlConnection, and dispose of it after we are done.
        Dim cn As New SqlConnection(connectionString)
        Try
            cn.Open()

            'call the overload that takes a connection in place of the connection string
            Return ExecuteDataset(cn, commandType, commandText, commandParameters)
        Finally
            cn.Dispose()
        End Try
    End Function 'ExecuteDataset

    Public Overloads Shared Function ExecuteDataset(ByVal connectionString As String,
                                                    ByVal spName As String,
                                                    ByVal ParamArray parameterValues() As Object) As DataSet

        Dim commandParameters As SqlParameter()

        'if we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) And parameterValues.Length > 0 Then
            'pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)

            'assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues)

            'call the overload that takes an array of SqlParameters
            Return ExecuteDataset(connectionString, CommandType.StoredProcedure, spName, commandParameters)
            'otherwise we can just call the SP without params
        Else
            Return ExecuteDataset(connectionString, CommandType.StoredProcedure, spName)
        End If
    End Function 'ExecuteDataset

    Public Overloads Shared Function ExecuteDataset(ByVal connection As SqlConnection,
                                                    ByVal commandType As CommandType,
                                                    ByVal commandText As String) As DataSet

        'pass through the call providing null for the set of SqlParameters
        Return ExecuteDataset(connection, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function 'ExecuteDataset

    Public Overloads Shared Function ExecuteDataset(ByVal connection As SqlConnection,
                                                    ByVal commandType As CommandType,
                                                    ByVal commandText As String,
                                                    ByVal ParamArray commandParameters() As SqlParameter) As DataSet

        'create a command and prepare it for execution
        Dim cmd As New SqlCommand
        Dim ds As New DataSet
        Dim da As SqlDataAdapter

        PrepareCommand(cmd, connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters)

        'create the DataAdapter & DataSet
        da = New SqlDataAdapter(cmd)

        'fill the DataSet using default values for DataTable names, etc.
        da.Fill(ds)

        'detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()

        'return the dataset
        Return ds

    End Function 'ExecuteDataset

    Public Overloads Shared Function ExecuteDataset(ByVal connection As SqlConnection,
                                                    ByVal spName As String,
                                                    ByVal ParamArray parameterValues() As Object) As DataSet
        'Return ExecuteDataset(connection, spName, parameterValues)
        Dim commandParameters As SqlParameter()

        'if we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) And parameterValues.Length > 0 Then
            'pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection.ConnectionString, spName)

            'assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues)

            'call the overload that takes an array of SqlParameters
            Return ExecuteDataset(connection, CommandType.StoredProcedure, spName, commandParameters)
            'otherwise we can just call the SP without params
        Else
            Return ExecuteDataset(connection, CommandType.StoredProcedure, spName)
        End If

    End Function 'ExecuteDataset

    Public Overloads Shared Function ExecuteDataset(ByVal transaction As SqlTransaction,
                                                    ByVal commandType As CommandType,
                                                    ByVal commandText As String) As DataSet
        'pass through the call providing null for the set of SqlParameters
        Return ExecuteDataset(transaction, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function 'ExecuteDataset

    Public Overloads Shared Function ExecuteDataset(ByVal transaction As SqlTransaction,
                                                    ByVal commandType As CommandType,
                                                    ByVal commandText As String,
                                                    ByVal ParamArray commandParameters() As SqlParameter) As DataSet
        'create a command and prepare it for execution
        Dim cmd As New SqlCommand
        Dim ds As New DataSet
        Dim da As SqlDataAdapter

        PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters)

        'create the DataAdapter & DataSet
        da = New SqlDataAdapter(cmd)

        'fill the DataSet using default values for DataTable names, etc.
        da.Fill(ds)

        'detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()

        'return the dataset
        Return ds
    End Function 'ExecuteDataset

    Public Overloads Shared Function ExecuteDataset(ByVal transaction As SqlTransaction,
                                                    ByVal spName As String,
                                                    ByVal ParamArray parameterValues() As Object) As DataSet
        Dim commandParameters As SqlParameter()

        'if we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) And parameterValues.Length > 0 Then
            'pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection.ConnectionString, spName)

            'assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues)

            'call the overload that takes an array of SqlParameters
            Return ExecuteDataset(transaction, CommandType.StoredProcedure, spName, commandParameters)
            'otherwise we can just call the SP without params
        Else
            Return ExecuteDataset(transaction, CommandType.StoredProcedure, spName)
        End If
    End Function 'ExecuteDataset


#End Region

#Region "ExecuteReader"
    Private Enum SqlConnectionOwnership
        'Connection is owned and managed by SqlHelper
        Internal
        'Connection is owned and managed by the caller
        [External]
    End Enum 'SqlConnectionOwnership

    Private Overloads Shared Function ExecuteReader(ByVal connection As SqlConnection,
                                                    ByVal transaction As SqlTransaction,
                                                    ByVal commandType As CommandType,
                                                    ByVal commandText As String,
                                                    ByVal commandParameters() As SqlParameter,
                                                    ByVal connectionOwnership As SqlConnectionOwnership) As SqlDataReader
        'create a command and prepare it for execution
        Dim cmd As New SqlCommand
        'create a reader
        Dim dr As SqlDataReader

        PrepareCommand(cmd, connection, transaction, commandType, commandText, commandParameters)

        ' call ExecuteReader with the appropriate CommandBehavior
        If connectionOwnership = SqlConnectionOwnership.External Then
            dr = cmd.ExecuteReader()
        Else
            dr = cmd.ExecuteReader(CommandBehavior.CloseConnection)
        End If

        'detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()

        Return dr
    End Function 'ExecuteReader

    Public Overloads Shared Function ExecuteReader(ByVal connectionString As String,
                                                   ByVal commandType As CommandType,
                                                   ByVal commandText As String) As SqlDataReader
        'pass through the call providing null for the set of SqlParameters
        Return ExecuteReader(connectionString, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function 'ExecuteReader

    Public Overloads Shared Function ExecuteReader(ByVal connectionString As String,
                                                   ByVal commandType As CommandType,
                                                   ByVal commandText As String,
                                                   ByVal ParamArray commandParameters() As SqlParameter) As SqlDataReader
        'create & open a SqlConnection
        Dim cn As New SqlConnection(connectionString)
        cn.Open()

        Try
            'call the private overload that takes an internally owned connection in place of the connection string
            Return ExecuteReader(cn, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters, SqlConnectionOwnership.Internal)
        Catch
            'if we fail to return the SqlDatReader, we need to close the connection ourselves
            cn.Dispose()
        End Try
    End Function 'ExecuteReader

    Public Overloads Shared Function ExecuteReader(ByVal connectionString As String,
                                                   ByVal spName As String,
                                                   ByVal ParamArray parameterValues() As Object) As SqlDataReader
        Dim commandParameters As SqlParameter()

        'if we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) And parameterValues.Length > 0 Then
            'pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)

            'assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues)

            'call the overload that takes an array of SqlParameters
            Return ExecuteReader(connectionString, CommandType.StoredProcedure, spName, commandParameters)
            'otherwise we can just call the SP without params
        Else
            Return ExecuteReader(connectionString, CommandType.StoredProcedure, spName)
        End If
    End Function 'ExecuteReader

    Public Overloads Shared Function ExecuteReader(ByVal connection As SqlConnection,
                                                   ByVal commandType As CommandType,
                                                   ByVal commandText As String) As SqlDataReader

        Return ExecuteReader(connection, commandType, commandText, CType(Nothing, SqlParameter()))

    End Function 'ExecuteReader

    Public Overloads Shared Function ExecuteReader(ByVal connection As SqlConnection,
                                                   ByVal commandType As CommandType,
                                                   ByVal commandText As String,
                                                   ByVal ParamArray commandParameters() As SqlParameter) As SqlDataReader
        'pass through the call to private overload using a null transaction value
        Return ExecuteReader(connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters, SqlConnectionOwnership.External)

    End Function 'ExecuteReader

    Public Overloads Shared Function ExecuteReader(ByVal connection As SqlConnection,
                                                   ByVal spName As String,
                                                   ByVal ParamArray parameterValues() As Object) As SqlDataReader
        'pass through the call using a null transaction value
        'Return ExecuteReader(connection, CType(Nothing, SqlTransaction), spName, parameterValues)

        Dim commandParameters As SqlParameter()

        'if we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) And parameterValues.Length > 0 Then
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection.ConnectionString, spName)

            AssignParameterValues(commandParameters, parameterValues)

            ' REMOVED BY CDW...
            'Return ExecuteReader(CommandType.StoredProcedure, spName, commandParameters)
            'otherwise we can just call the SP without params
        Else
            Return ExecuteReader(connection, CommandType.StoredProcedure, spName)
        End If

    End Function 'ExecuteReader

    Public Overloads Shared Function ExecuteReader(ByVal transaction As SqlTransaction,
                                                   ByVal commandType As CommandType,
                                                   ByVal commandText As String) As SqlDataReader
        'pass through the call providing null for the set of SqlParameters
        Return ExecuteReader(transaction, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function 'ExecuteReader

    Public Overloads Shared Function ExecuteReader(ByVal transaction As SqlTransaction,
                                                   ByVal commandType As CommandType,
                                                   ByVal commandText As String,
                                                   ByVal ParamArray commandParameters() As SqlParameter) As SqlDataReader
        'pass through to private overload, indicating that the connection is owned by the caller
        Return ExecuteReader(transaction.Connection, transaction, commandType, commandText, commandParameters, SqlConnectionOwnership.External)
    End Function 'ExecuteReader

    Public Overloads Shared Function ExecuteReader(ByVal transaction As SqlTransaction,
                                                   ByVal spName As String,
                                                   ByVal ParamArray parameterValues() As Object) As SqlDataReader
        Dim commandParameters As SqlParameter()

        'if we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) And parameterValues.Length > 0 Then
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection.ConnectionString, spName)

            AssignParameterValues(commandParameters, parameterValues)

            Return ExecuteReader(transaction, CommandType.StoredProcedure, spName, commandParameters)
            'otherwise we can just call the SP without params
        Else
            Return ExecuteReader(transaction, CommandType.StoredProcedure, spName)
        End If
    End Function 'ExecuteReader

#End Region

#Region "ExecuteScalar"

    Public Overloads Shared Function ExecuteScalar(ByVal connectionString As String,
                                                   ByVal commandType As CommandType,
                                                   ByVal commandText As String) As Object
        'pass through the call providing null for the set of SqlParameters
        Return ExecuteScalar(connectionString, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function 'ExecuteScalar

    Public Overloads Shared Function ExecuteScalar(ByVal connectionString As String,
                                                   ByVal commandType As CommandType,
                                                   ByVal commandText As String,
                                                   ByVal ParamArray commandParameters() As SqlParameter) As Object
        'create & open a SqlConnection, and dispose of it after we are done.
        Dim cn As New SqlConnection(connectionString)
        Try
            cn.Open()

            'call the overload that takes a connection in place of the connection string
            Return ExecuteScalar(cn, commandType, commandText, commandParameters)
        Finally
            Try
                cn.Dispose()
            Catch
            End Try
        End Try
    End Function 'ExecuteScalar

    Public Overloads Shared Function ExecuteScalar(ByVal connectionString As String,
                                                   ByVal spName As String,
                                                   ByVal ParamArray parameterValues() As Object) As Object
        Dim commandParameters As SqlParameter()

        'if we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) And parameterValues.Length > 0 Then
            'pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName)

            'assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues)

            'call the overload that takes an array of SqlParameters
            Return ExecuteScalar(connectionString, CommandType.StoredProcedure, spName, commandParameters)
            'otherwise we can just call the SP without params
        Else
            Return ExecuteScalar(connectionString, CommandType.StoredProcedure, spName)
        End If
    End Function 'ExecuteScalar

    Public Overloads Shared Function ExecuteScalar(ByVal connection As SqlConnection,
                                                   ByVal commandType As CommandType,
                                                   ByVal commandText As String) As Object
        'pass through the call providing null for the set of SqlParameters
        Return ExecuteScalar(connection, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function 'ExecuteScalar

    Public Overloads Shared Function ExecuteScalar(ByVal connection As SqlConnection,
                                                   ByVal commandType As CommandType,
                                                   ByVal commandText As String,
                                                   ByVal ParamArray commandParameters() As SqlParameter) As Object
        'create a command and prepare it for execution
        Dim cmd As New SqlCommand
        Dim retval As Object

        PrepareCommand(cmd, connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters)

        'execute the command & return the results
        retval = cmd.ExecuteScalar()

        'detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()

        Return retval

    End Function 'ExecuteScalar

    Public Overloads Shared Function ExecuteScalar(ByVal connection As SqlConnection,
                                                   ByVal spName As String,
                                                   ByVal ParamArray parameterValues() As Object) As Object

        Dim commandParameters As SqlParameter()

        'if we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) And parameterValues.Length > 0 Then
            'pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection.ConnectionString, spName)

            'assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues)

            'call the overload that takes an array of SqlParameters
            Return ExecuteScalar(connection, CommandType.StoredProcedure, spName, commandParameters)
            'otherwise we can just call the SP without params
        Else
            Return ExecuteScalar(connection, CommandType.StoredProcedure, spName)
        End If

    End Function 'ExecuteScalar

    Public Overloads Shared Function ExecuteScalar(ByVal transaction As SqlTransaction,
                                                   ByVal commandType As CommandType,
                                                   ByVal commandText As String) As Object
        'pass through the call providing null for the set of SqlParameters
        Return ExecuteScalar(transaction, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function 'ExecuteScalar

    Public Overloads Shared Function ExecuteScalar(ByVal transaction As SqlTransaction,
                                                   ByVal commandType As CommandType,
                                                   ByVal commandText As String,
                                                   ByVal ParamArray commandParameters() As SqlParameter) As Object
        'create a command and prepare it for execution
        Dim cmd As New SqlCommand
        Dim retval As Object

        PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters)

        'execute the command & return the results
        retval = cmd.ExecuteScalar()

        'detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()

        Return retval

    End Function 'ExecuteScalar

    Public Overloads Shared Function ExecuteScalar(ByVal transaction As SqlTransaction,
                                                   ByVal spName As String,
                                                   ByVal ParamArray parameterValues() As Object) As Object
        Dim commandParameters As SqlParameter()

        'if we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) And parameterValues.Length > 0 Then
            'pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection.ConnectionString, spName)

            'assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues)

            'call the overload that takes an array of SqlParameters
            Return ExecuteScalar(transaction, CommandType.StoredProcedure, spName, commandParameters)
            'otherwise we can just call the SP without params
        Else
            Return ExecuteScalar(transaction, CommandType.StoredProcedure, spName)
        End If
    End Function 'ExecuteScalar

#End Region

#Region "ExecuteXmlReader"

    Public Overloads Shared Function ExecuteXmlReader(ByVal connection As SqlConnection,
                                                      ByVal commandType As CommandType,
                                                      ByVal commandText As String) As XmlReader
        'pass through the call providing null for the set of SqlParameters
        Return ExecuteXmlReader(connection, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function 'ExecuteXmlReader

    Public Overloads Shared Function ExecuteXmlReader(ByVal connection As SqlConnection,
                                                      ByVal commandType As CommandType,
                                                      ByVal commandText As String,
                                                      ByVal ParamArray commandParameters() As SqlParameter) As XmlReader
        'create a command and prepare it for execution
        Dim cmd As New SqlCommand
        Dim retval As XmlReader

        PrepareCommand(cmd, connection, CType(Nothing, SqlTransaction), commandType, commandText, commandParameters)

        'create the DataAdapter & DataSet
        retval = cmd.ExecuteXmlReader()

        'detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()

        Return retval


    End Function 'ExecuteXmlReader

    Public Overloads Shared Function ExecuteXmlReader(ByVal connection As SqlConnection,
                                                      ByVal spName As String,
                                                      ByVal ParamArray parameterValues() As Object) As XmlReader
        Dim commandParameters As SqlParameter()

        'if we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) And parameterValues.Length > 0 Then
            'pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection.ConnectionString, spName)

            'assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues)

            'call the overload that takes an array of SqlParameters
            Return ExecuteXmlReader(connection, CommandType.StoredProcedure, spName, commandParameters)
            'otherwise we can just call the SP without params
        Else
            Return ExecuteXmlReader(connection, CommandType.StoredProcedure, spName)
        End If
    End Function 'ExecuteXmlReader

    Public Overloads Shared Function ExecuteXmlReader(ByVal transaction As SqlTransaction,
                                                      ByVal commandType As CommandType,
                                                      ByVal commandText As String) As XmlReader
        'pass through the call providing null for the set of SqlParameters
        Return ExecuteXmlReader(transaction, commandType, commandText, CType(Nothing, SqlParameter()))
    End Function 'ExecuteXmlReader

    Public Overloads Shared Function ExecuteXmlReader(ByVal transaction As SqlTransaction,
                                                      ByVal commandType As CommandType,
                                                      ByVal commandText As String,
                                                      ByVal ParamArray commandParameters() As SqlParameter) As XmlReader
        'create a command and prepare it for execution
        Dim cmd As New SqlCommand
        Dim retval As XmlReader

        PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters)

        'create the DataAdapter & DataSet
        retval = cmd.ExecuteXmlReader()

        'detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear()

        Return retval

    End Function 'ExecuteXmlReader

    Public Overloads Shared Function ExecuteXmlReader(ByVal transaction As SqlTransaction,
                                                      ByVal spName As String,
                                                      ByVal ParamArray parameterValues() As Object) As XmlReader
        Dim commandParameters As SqlParameter()

        'if we receive parameter values, we need to figure out where they go
        If Not (parameterValues Is Nothing) And parameterValues.Length > 0 Then
            'pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection.ConnectionString, spName)

            'assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues)

            'call the overload that takes an array of SqlParameters
            Return ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName, commandParameters)
            'otherwise we can just call the SP without params
        Else
            Return ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName)
        End If
    End Function 'ExecuteXmlReader

#End Region

End Class 'SqlHelper

Public NotInheritable Class SqlHelperParameterCache

#Region "private methods, variables, and constructors"


    'Since this class provides only static methods, make the default constructor private to prevent 
    'instances from being created with "new SqlHelperParameterCache()".
    Private Sub New()
    End Sub 'New 

    Private Shared paramCache As Hashtable = Hashtable.Synchronized(New Hashtable())

    Private Shared Function DiscoverSpParameterSet(ByVal connectionString As String,
                                                   ByVal spName As String,
                                                   ByVal includeReturnValueParameter As Boolean,
                                                   ByVal ParamArray parameterValues() As Object) As SqlParameter()

        Dim cn As New SqlConnection(connectionString)
        Dim cmd As SqlCommand = New SqlCommand(spName, cn)
        Dim discoveredParameters() As SqlParameter

        Try
            cn.Open()
            cmd.CommandType = CommandType.StoredProcedure
            SqlCommandBuilder.DeriveParameters(cmd)
            If Not includeReturnValueParameter Then
                cmd.Parameters.RemoveAt(0)
            End If

            discoveredParameters = New SqlParameter(cmd.Parameters.Count - 1) {}
            cmd.Parameters.CopyTo(discoveredParameters, 0)
        Finally
            cmd.Dispose()
            cn.Dispose()

        End Try

        Return discoveredParameters

    End Function 'DiscoverSpParameterSet

    'deep copy of cached SqlParameter array
    Private Shared Function CloneParameters(ByVal originalParameters() As SqlParameter) As SqlParameter()

        Dim i As Short
        Dim j As Short = originalParameters.Length - 1
        Dim clonedParameters(j) As SqlParameter

        For i = 0 To j
            clonedParameters(i) = CType(CType(originalParameters(i), ICloneable).Clone, SqlParameter)
        Next

        Return clonedParameters
    End Function 'CloneParameters



#End Region

#Region "caching functions"

    Public Shared Sub CacheParameterSet(ByVal connectionString As String,
                                        ByVal commandText As String,
                                        ByVal ParamArray commandParameters() As SqlParameter)
        Dim hashKey As String = connectionString + ":" + commandText

        paramCache(hashKey) = commandParameters
    End Sub 'CacheParameterSet

    Public Shared Function GetCachedParameterSet(ByVal connectionString As String, ByVal commandText As String) As SqlParameter()
        Dim hashKey As String = connectionString + ":" + commandText
        Dim cachedParameters As SqlParameter() = CType(paramCache(hashKey), SqlParameter())

        If cachedParameters Is Nothing Then
            Return Nothing
        Else
            Return CloneParameters(cachedParameters)
        End If
    End Function 'GetCachedParameterSet

#End Region

#Region "Parameter Discovery Functions"
    Public Overloads Shared Function GetSpParameterSet(ByVal connectionString As String, ByVal spName As String) As SqlParameter()
        Return GetSpParameterSet(connectionString, spName, False)
    End Function 'GetSpParameterSet 

    Public Overloads Shared Function GetSpParameterSet(ByVal connectionString As String,
                                                       ByVal spName As String,
                                                       ByVal includeReturnValueParameter As Boolean) As SqlParameter()

        Dim cachedParameters() As SqlParameter
        Dim hashKey As String

        hashKey = connectionString + ":" + spName + IIf(includeReturnValueParameter = True, ":include ReturnValue Parameter", "")

        cachedParameters = CType(paramCache(hashKey), SqlParameter())

        If (cachedParameters Is Nothing) Then
            paramCache(hashKey) = DiscoverSpParameterSet(connectionString, spName, includeReturnValueParameter)
            cachedParameters = CType(paramCache(hashKey), SqlParameter())

        End If

        Return CloneParameters(cachedParameters)

    End Function 'GetSpParameterSet
#End Region

End Class
