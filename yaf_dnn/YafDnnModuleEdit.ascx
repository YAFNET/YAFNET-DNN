<%@ Control language="c#" Inherits="YAF.DotNetNuke.YafDnnModuleEdit" CodeBehind="YafDnnModuleEdit.ascx.cs" AutoEventWireup="False" %>
<%@ Import Namespace="DotNetNuke.Services.Localization" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>

<script type="text/javascript">
    jQuery(function ($) {
        var setupModule = function () {
            $('#yaf-settings').dnnPanels();
            $('#yaf-settings .dnnFormExpandContent a').dnnExpandAll({
                targetArea: '#yaf-settings'
            });
        };
        setupModule();
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
            setupModule();
        });
    });
</script>

<div class="dnnForm" id="yaf-settings">
    <div class="dnnFormExpandContent">
        <a href=""><%=Localization.GetString("ExpandAll", Localization.SharedResourceFile)%></a>
    </div>
    <h2 id="SelectBoard" class="dnnFormSectionHead">
        <a href="#">
            <%= this.LocalizeString("CreateOrSelect")%>
        </a>
    </h2>
    <fieldset class="dnnClear">
        <div class="dnnFormItem">
            <dnn:label id="BoardName" runat="server" controlname="BoardID" Suffix="?"></dnn:label>
            <asp:dropdownlist autopostback="true" runat="server" id="BoardID" />&nbsp;
        </div>
        <hr/>
        <div class="dnnFormItem">
            <dnn:label id="NewBoard" runat="server" controlname="NewBoardName" Suffix="?"></dnn:label>
            <asp:TextBox runat="server" ID="NewBoardName"></asp:TextBox>
            <asp:LinkButton runat="server" id="create" CssClass="dnnPrimaryAction" OnClick="Create_OnClick" />
        </div>
        <hr/>
        <asp:PlaceHolder runat="server" ID="ActiveForumsPlaceHolder">
        <div class="dnnFormItem">
            <dnn:label id="ActiveForumsImport" runat="server" controlname="ActiveForums" Suffix="?"></dnn:label>
            <asp:DropDownList id="ActiveForums" runat="server" 
                datavaluefield="ModuleID" datatextfield="ModuleTitle">
            </asp:DropDownList>
            <asp:LinkButton runat="server" id="ImportForums" CssClass="dnnPrimaryAction" OnClick="ImportForums_OnClick" />
        <div class="dnnFormMessage dnnFormInfo"><%= this.LocalizeString("NoteAF")%></div>
        </div>
        </asp:PlaceHolder>
    </fieldset>
    <h2 id="Other" class="dnnFormSectionHead"><a href="#">
        <%= this.LocalizeString("OtherSettings")%>
    </a></h2>
        <fieldset class="dnnClear">
    <div class="dnnFormItem">
        <dnn:label id="RemoveTabNameLabel" runat="server" controlname="RemoveTabName" Suffix=":"></dnn:label>
        <asp:dropdownlist runat="server" id="RemoveTabName" />
    </div>
    <div class="dnnFormItem">
        <dnn:label id="InheritLanguage" runat="server" controlname="InheritDnnLanguage" Suffix=":"></dnn:label>
        <asp:CheckBox runat="server" id="InheritDnnLanguage"  />
    </div>
            </fieldset>
    </div>
    <ul class="dnnActions dnnClear">
        <li>
            <asp:LinkButton runat="server" id="update" cssclass="dnnPrimaryAction" />
        </li>
        <li>
            <asp:LinkButton runat="server" id="cancel" cssclass="dnnSecondaryAction" />
        </li>
    </ul>