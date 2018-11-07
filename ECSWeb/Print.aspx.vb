
Partial Class Print
    Inherits System.Web.UI.Page

    Private Sub form1_Load(sender As Object, e As EventArgs) Handles form1.Load

    End Sub

    Private Sub cmdPrintLabel_Click(sender As Object, e As EventArgs) Handles cmdPrintLabel.Click
        'Declare a BarTender application variable 
        Dim btApp As BarTender.Application
        Dim btPrintRtn As BarTender.BtPrintResult
        Dim btMsgs As BarTender.Messages = Nothing
        Dim strPrinter As String = "Adobe PDF"
        'Dim strPrinter As String = "PDFCreator"
        Dim strDirectory As String = "C:\Users\ChrisWilfong\Documents\Visual Studio 2017\Projects\ECSWeb\ECSWeb"
        Dim strOutputFilenameDir As String = strDirectory & "\Company\ECS\Output\"
        Dim strOutputPreviewDir As String = strDirectory & "\Company\ECS\Preview"
        Dim strLabelName As String = strDirectory & "\Company\ECS\Labels\" & DropDownListLabels.SelectedValue & ".btw"
        Dim strPrinterFileName As String
        Dim strStamp As String
        Dim strSQL As String

        'strPrinterFileName = strOutputFilenameDir & "\" & Me.txtOutputFileName.Text & ".pdf"
        strPrinterFileName = Me.txtOutputFileName.Text & ".pdf"

        'Declare a BarTender document variable 
        Dim btFormat As BarTender.Format
        Dim btDb As BarTender.Database
        btApp = New BarTender.Application

        'Open a BarTender document 
        btFormat = btApp.Formats.Open(strLabelName, True, strPrinter)

        btFormat.Printer = strPrinter
        btDb = btFormat.Databases.Item("ECS")
        'btDb.SQLStatement = "SELECT TOP " & CInt(Me.numRecs.Text) & " * From tblTPR_PriceChg Where TagName = 'TPR'"
        btDb.SQLStatement = "SELECT * FROM tblTPR_PriceChg WHERE TagName = '" & DropDownListLabels.SelectedValue & "' AND StoreNumber = " & DropDownListStore.SelectedValue

        ' PREVIEW INDIVIDUAL PAGES...
        'btFormat.ExportPrintPreviewRangeToImage("1-500", strOutputFilenameDir, Me.txtOutputFileName.Text & ".ps", "EPS", BarTender.BtColors.btColors24Bit, 300, 16777215, BarTender.BtSaveOptions.btSaveChanges, False, False, Nothing)
        'btFormat.ExportPrintPreviewRangeToImage("1-500", strOutputPreviewDir, Me.txtOutputFileName.Text & ".jpg", "JPG", BarTender.BtColors.btColors24Bit, 300, 16777215, BarTender.BtSaveOptions.btSaveChanges, False, False, Nothing)

        ' PRINT OUT...
        'Create a new print to file license key 
        If 1 = 2 Then
            Dim printCLient As New BarTenderPrintClientWPS.Printer
            Dim printLicense As String = Nothing
            printCLient.CreatePrintToFileLicense(strPrinter, printLicense)
            btFormat.PrintToFileLicense = printLicense

            btFormat.PrintToFile = True
            btFormat.PrinterFile = strOutputFilenameDir & "\" & Me.txtOutputFileName.Text & ".prn"
            btFormat.PrintOut(False, False)
        End If

        ' PRINT...
        If 1 = 1 Then
            ' Replace the picture if needed...
            If DropDownListLabels.SelectedValue = "TPR1" Then

            End If
            ' Get the name of the PDF...
            strStamp = Now.DayOfYear & Now.Hour & Now.Minute & Now.Second & Now.Millisecond
            strPrinterFileName = DropDownListLabels.SelectedValue & "_" & DropDownListStore.SelectedValue & "_" & strStamp & ".pdf"

            ' Preview...
            btFormat.ExportPrintPreviewRangeToImage("1", strOutputPreviewDir, strPrinterFileName.Replace(".pdf", ".jpg"), "JPG", BarTender.BtColors.btColors24Bit, 300, 16777215, BarTender.BtSaveOptions.btSaveChanges, False, False, Nothing)

            ' Print it...
            btFormat.PrintToFile = False
            btFormat.PrinterFile = strPrinterFileName
            btPrintRtn = btFormat.Print(strPrinterFileName, False, -1, btMsgs)

            ' Insert record to DB and refresh grid...
            strSQL = "INSERT INTO tblPDFLog (strUserName, strFormatName, strPDFFile, strSQLStatement, strFileName) VALUES "
            strSQL = strSQL & "('CHRIS', '" & Me.DropDownListLabels.SelectedValue & "', '" & ("\Company\ECS\Output\" & strPrinterFileName) & "', '" & Replace(btDb.SQLStatement, "'", "") & "', '" & strPrinterFileName & "')"
            SQLHelper.ExecuteNonQuery(SQLHelper.SQLConnection, System.Data.CommandType.Text, strSQL)
            RadGrid1.Rebind()
            ' Preview image...
            imgPreview.ImageUrl = "\Company\ECS\Preview\" & strPrinterFileName.Replace(".pdf", "1.jpg")
        End If

        'End the BarTender process 
        btApp.Quit(BarTender.BtSaveOptions.btDoNotSaveChanges)

        lblMessage.Text = "Done"
    End Sub

    Private Sub cmdRefreshGrid_Click(sender As Object, e As EventArgs) Handles cmdRefreshGrid.Click
        RadGrid1.Rebind()
        DropDownListLabels.DataBind()
        DropDownListStore.DataBind()
    End Sub
End Class