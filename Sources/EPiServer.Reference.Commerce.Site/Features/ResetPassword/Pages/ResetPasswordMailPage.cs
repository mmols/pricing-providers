using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Reference.Commerce.Site.Features.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace EPiServer.Reference.Commerce.Site.Features.ResetPassword.Pages
{
    [ContentType(DisplayName = "Reset password e-mail", GUID = "6c939f47-b8bb-44a8-bc10-371b54d609b4", Description = "E-mail used when resetting a user's password.", AvailableInEditMode = true)]
    public class ResetPasswordMailPage : MailBasePage
    {
        [CultureSpecific]
        [Display(
            Name = "Main body",
            Description = "The main body will be shown in the main content area of the page, using the XHTML-editor you can insert for example text, images and tables.",
            GroupName = SystemTabNames.Content,
            Order = 1)]
        public virtual string MailBody { get; set; }
    }
}