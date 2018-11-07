<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Print.aspx.vb" Inherits="Print" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="telerik" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div>
            <asp:Label runat="server" ID="Label1" Text="Tag Name : " Width="150px" Font-Bold="true"></asp:Label>
            <asp:DropDownList runat="server" ID="DropDownListLabels" DataSourceID="SqlDataSource1" DataValueField="TagName" DataTextField="TagName" Width="150px" ></asp:DropDownList><br />
            <asp:Label runat="server" ID="Label2" Text="Store : " Width="150px" Font-Bold="true"></asp:Label>
            <asp:DropDownList runat="server" ID="DropDownListStore" DataSourceID="SqlDataSource2" DataValueField="StoreNumber" DataTextField="StoreNumber" Width="150px" ></asp:DropDownList><br />
            <asp:Button runat="server" ID="cmdPrintLabel" Text="Print Label"></asp:Button><br />
            <asp:TextBox runat="server" ID="numRecs" Text="5" Visible="false"></asp:TextBox><br />
            <asp:TextBox runat="server" ID="txtOutputFileName" Text="TestPrint001" Visible="false"></asp:TextBox>
            <asp:Label ID="lblMessage" runat="server" ForeColor="Red"></asp:Label>
            <br /><br />
            <asp:Button runat="server" ID="cmdRefreshGrid" Text="Refresh The Grid AGAIN and yet AGAIN" />
            <telerik:RadGrid ID="RadGrid1" runat="server" DataSourceID="SqlDataSource3" AutoGenerateColumns="false" Width="50%">
                        <MasterTableView DataSourceID="SqlDataSource3" TableLayout="Fixed" AllowSorting="false" AllowFilteringByColumn="false">
                            <Columns>
                                <telerik:GridBoundColumn DataField="Id" UniqueName="Id" AllowFiltering="false" AllowSorting="false" Visible="false" >
                                </telerik:GridBoundColumn>
                                <telerik:GridBoundColumn DataField="strUserName" HeaderAbbr="User" UniqueName="strUserName" HeaderText="Username" 
                                    AllowFiltering="false" AllowSorting="false" >
                                    <HeaderStyle Font-Bold="true" />
                                </telerik:GridBoundColumn>
                                <telerik:GridHyperLinkColumn DataNavigateUrlFields="strPDFFile" DataTextField="strFileName" DataNavigateUrlFormatString="{0}" HeaderAbbr="Format" headerText="Format" Target="_blank">
                                    <HeaderStyle Font-Bold="true" />
                                </telerik:GridHyperLinkColumn>
                                <telerik:GridBoundColumn DataField="dtmStamp" HeaderAbbr="Date/Time" UniqueName="dtmStamp" HeaderText="Date/Time" 
                                    AllowFiltering="false" AllowSorting="false" >
                                    <HeaderStyle Font-Bold="true" />
                                </telerik:GridBoundColumn>
                                <telerik:GridCheckBoxColumn DataField="ysnActive"></telerik:GridCheckBoxColumn>
                            </Columns>                        
                        </MasterTableView>                
            </telerik:RadGrid>
            <br />
            <asp:Image runat="server" ID="imgPreview" Width="400" />
        </div>
    </form>
</body>
</html>

<asp:SqlDataSource ID="SqlDataSource1" runat="server" 
    ConnectionString="<%$ ConnectionStrings:ECSConnectionString %>" 
    SelectCommand="SELECT DISTINCT TagName FROM tblTPR_PriceChg ORDER BY TagName">
</asp:SqlDataSource>

<asp:SqlDataSource ID="SqlDataSource2" runat="server" 
    ConnectionString="<%$ ConnectionStrings:ECSConnectionString %>" 
    SelectCommand="SELECT DISTINCT StoreNumber FROM tblTPR_PriceChg ORDER BY StoreNumber">
</asp:SqlDataSource>

<asp:SqlDataSource ID="SqlDataSource3" runat="server" 
    ConnectionString="<%$ ConnectionStrings:ECSConnectionString %>" 
    SelectCommand="SELECT * FROM tblPDFLog WHERE ysnActive = 1 ORDER BY dtmStamp DESC">
</asp:SqlDataSource>
