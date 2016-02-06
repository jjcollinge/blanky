using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceRouter.Controllers
{
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/2011/01/fabric")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://schemas.microsoft.com/2011/01/fabric", IsNullable = false)]
    public partial class ApplicationManifest
    {

        private object parametersField;

        private ApplicationManifestServiceManifestImport serviceManifestImportField;

        private ApplicationManifestDefaultServices defaultServicesField;

        private string applicationTypeNameField;

        private string applicationTypeVersionField;

        /// <remarks/>
        public object Parameters
        {
            get
            {
                return this.parametersField;
            }
            set
            {
                this.parametersField = value;
            }
        }

        /// <remarks/>
        public ApplicationManifestServiceManifestImport ServiceManifestImport
        {
            get
            {
                return this.serviceManifestImportField;
            }
            set
            {
                this.serviceManifestImportField = value;
            }
        }

        /// <remarks/>
        public ApplicationManifestDefaultServices DefaultServices
        {
            get
            {
                return this.defaultServicesField;
            }
            set
            {
                this.defaultServicesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ApplicationTypeName
        {
            get
            {
                return this.applicationTypeNameField;
            }
            set
            {
                this.applicationTypeNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ApplicationTypeVersion
        {
            get
            {
                return this.applicationTypeVersionField;
            }
            set
            {
                this.applicationTypeVersionField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/2011/01/fabric")]
    public partial class ApplicationManifestServiceManifestImport
    {

        private ApplicationManifestServiceManifestImportServiceManifestRef serviceManifestRefField;

        /// <remarks/>
        public ApplicationManifestServiceManifestImportServiceManifestRef ServiceManifestRef
        {
            get
            {
                return this.serviceManifestRefField;
            }
            set
            {
                this.serviceManifestRefField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/2011/01/fabric")]
    public partial class ApplicationManifestServiceManifestImportServiceManifestRef
    {

        private string serviceManifestNameField;

        private string serviceManifestVersionField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ServiceManifestName
        {
            get
            {
                return this.serviceManifestNameField;
            }
            set
            {
                this.serviceManifestNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ServiceManifestVersion
        {
            get
            {
                return this.serviceManifestVersionField;
            }
            set
            {
                this.serviceManifestVersionField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/2011/01/fabric")]
    public partial class ApplicationManifestDefaultServices
    {

        private ApplicationManifestDefaultServicesService serviceField;

        /// <remarks/>
        public ApplicationManifestDefaultServicesService Service
        {
            get
            {
                return this.serviceField;
            }
            set
            {
                this.serviceField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/2011/01/fabric")]
    public partial class ApplicationManifestDefaultServicesService
    {

        private ApplicationManifestDefaultServicesServiceStatelessService statelessServiceField;

        private string nameField;

        /// <remarks/>
        public ApplicationManifestDefaultServicesServiceStatelessService StatelessService
        {
            get
            {
                return this.statelessServiceField;
            }
            set
            {
                this.statelessServiceField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://schemas.microsoft.com/2011/01/fabric")]
    public partial class ApplicationManifestDefaultServicesServiceStatelessService
    {

        private object singletonPartitionField;

        private string serviceTypeNameField;

        /// <remarks/>
        public object SingletonPartition
        {
            get
            {
                return this.singletonPartitionField;
            }
            set
            {
                this.singletonPartitionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ServiceTypeName
        {
            get
            {
                return this.serviceTypeNameField;
            }
            set
            {
                this.serviceTypeNameField = value;
            }
        }
    }
}
