<%@ Control Language="C#" AutoEventWireup="true" Inherits="Mediachase.Commerce.Manager.Content.Folders.FolderList" Codebehind="FolderList.ascx.cs" %>
<%@ Register Src="~/Apps/Core/Controls/EcfListViewControl.ascx" TagName="EcfListViewControl" TagPrefix="core" %>
<core:EcfListViewControl id="MyListView" runat="server" DataKey="PageId" AppId="Content" ViewId="Folder-List" ShowTopToolbar="true"></core:EcfListViewControl>