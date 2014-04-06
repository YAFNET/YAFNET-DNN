<%@ Control language="c#" Inherits="YAF.DotNetNuke.YafDnnModuleImport" CodeBehind="YafDnnModuleImport.ascx.cs" AutoEventWireup="False" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>

<div class="dnnForm">
    <div class="dnnFormItem">
        <dnn:label ID="lImport" runat="server" ResourceKey="lImport" Suffix=":"></dnn:label>
        <asp:LinkButton runat="server" id="btnImportUsers" CssClass="dnnPrimaryAction" />
    </div>
    <div class="dnnFormItem">
        <asp:Label ID="lInfo" runat="server" FontBold="true"></asp:Label>
    </div>
    <div class="dnnFormItem">
      <div style="width:100%;height:10px;border-top: 1px dotted black"></div>
    </div>
    <div class="dnnFormItem">
      <dnn:label id="lblAddScheduler" runat="server"  ResourceKey="lblAddScheduler" controlname="btnAddScheduler" suffix=":"></dnn:label>
      <asp:LinkButton id="btnAddScheduler" CommandArgument="add" runat="server" CssClass="dnnPrimaryAction"></asp:LinkButton>
    </div>
    <ul class="dnnActions dnnClear">
        <li>
            <asp:LinkButton runat="server" id="Close" CssClass="dnnPrimaryAction" />
        </li>
    </ul>
</div>