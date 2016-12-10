using Orchard.ContentManagement.MetaData;
using Orchard.ContentOperationalTransformation.Model;
using Orchard.DisplayManagement.Views;
using Orchard.ContentManagement.Display.ContentDisplay;

namespace Orchard.ContentOperationalTransformation.Drivers
{
    public class ContentOTPartDisplay : ContentPartDisplayDriver<ContentOTPart>
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;

        public ContentOTPartDisplay(IContentDefinitionManager contentDefinitionManager)
        {
            _contentDefinitionManager = contentDefinitionManager;
        }


        public override IDisplayResult Edit(ContentOTPart contentOTPart)
        {
            return Shape("ContentOTPart_Edit", contentOTPart);
        }
        
    }
}
