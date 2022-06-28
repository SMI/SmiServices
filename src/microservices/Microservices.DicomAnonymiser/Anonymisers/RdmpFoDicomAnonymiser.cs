using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.DataExport.DataExtraction.Pipeline;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.Repositories;
using Rdmp.Core.Startup;
using Rdmp.Dicom.Extraction.FoDicomBased;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using Smi.Common.Messages.Extraction;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservices.DicomAnonymiser.Anonymisers
{
    /// <summary>
    /// Anonymiser that instantiates a <see cref="PipelineComponent"/> which must be of 
    /// Type <see cref="FoDicomAnonymiser"/>
    /// </summary>
    public class RdmpFoDicomAnonymiser : IDicomAnonymiser
    {
        public int ID { get; }
        IRDMPPlatformRepositoryServiceLocator _repo;
        private FoDicomAnonymiser _anonymiserComponent;
        private ThrowImmediatelyDataLoadEventListener _listener;
        private ZipPool _zipPool;

        public RdmpFoDicomAnonymiser(GlobalOptions globals, int id)
        {
            ID = id;
            
            _repo = new LinkedRepositoryProvider(
            globals.RDMPOptions.CatalogueConnectionString,
            globals.RDMPOptions.DataExportConnectionString);

            var startup = new Startup(new EnvironmentInfo(), _repo);
            startup.DoStartup(new ThrowImmediatelyCheckNotifier());

            var component = _repo.CatalogueRepository.GetObjectByID<PipelineComponent>(id);

            var factory = new DataFlowPipelineEngineFactory(
                ExtractionPipelineUseCase.DesignTime(),
                _repo.CatalogueRepository.MEF);

            var instance = factory.TryCreateComponent(component, out Exception ex);
            
            if (ex != null)
            {
                throw new Exception($"Could not create PipelineComponent with ID {component.ID}", ex);
            }

            if(instance is not FoDicomAnonymiser f)
            {
                throw new Exception($"PipelineComponent with ID {component.ID} is a {component.GetType().Name}, expected it to be a {nameof(FoDicomAnonymiser)}");
            }

            _anonymiserComponent = f;
            _anonymiserComponent.Initialize(0, null);

            _listener = new ThrowImmediatelyDataLoadEventListener();
            _zipPool = new ZipPool();
        }


        public ExtractedFileStatus Anonymise(IFileInfo sourceFile, IFileInfo destFile)
        {
            _anonymiserComponent.ProcessFile(
                new AmbiguousFilePath(sourceFile.FullName), _listener, _zipPool, "ANON", new PutHere(destFile), null);

            return ExtractedFileStatus.Anonymised;
        }
    }
}
