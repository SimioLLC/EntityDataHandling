using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimioAPI;
using SimioAPI.Extensions;

namespace EntityDataSharing
{
    /// <summary>
    /// Just any dummy data.
    /// We'll create a large number of these just to demonstrate.
    /// </summary>
    public class DummyDataClass
    {
        public int DataKey { get; set; }

        public string FormattedStoreTime { get; set; }

        public DateTimeOffset CreationTime { get; set; }

        public double Value { get; set; }

        public DummyDataClass(int key, double value)
        {
            DataKey = key;
            Value = value;
            CreationTime = DateTimeOffset.Now;
            FormattedStoreTime = CreationTime.ToString("dd-MMM-yy HH:mm:ss.ffff");
        }
    }


    /// <summary>
    /// Information stored with each entity
    /// </summary>
    public class MyEntityData
    {
        /// <summary>
        /// Key value for the data, which is the Entity name.
        /// Fortunately, Entity names are unique within a Simulation run.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Just fabricated data, which could be anything.
        /// </summary>
        public List<DummyDataClass> DummyList { get; set; }

        /// <summary>
        /// Each time the enity visits a step it is put on this list.
        /// </summary>
        public List<string> TheStepsIHaveSeen { get; set; }

        /// <summary>
        /// The last time we were at a step (real time)
        /// </summary>
        public DateTimeOffset LastLocationTime { get; set; }

        /// <summary>
        ///  When the entity was created (real time)
        /// </summary>
        public DateTimeOffset CreationTime { get; set; }

        public MyEntityData(string key)
        {
            this.Key = key;
            DummyList = new List<DummyDataClass>();
            CreationTime = DateTimeOffset.Now;
            TheStepsIHaveSeen = new List<string>();
        }
    }


    class EntityWithDataStepDefinition : IStepDefinition
    {
        #region IStepDefinition Members

        /// <summary>
        /// Property returning the full name for this type of step. The name should contain no spaces.
        /// </summary>
        public string Name
        {
            get { return "EntityWithDataStep"; }
        }

        /// <summary>
        /// Property returning a short description of what the step does.
        /// </summary>
        public string Description
        {
            get { return "Allow a step to access data based on Entity name (using a dictionary within a Singleton object)."; }
        }

        /// <summary>
        /// Property returning an icon to display for the step in the UI.
        /// </summary>
        public System.Drawing.Image Icon
        {
            get { return null; }
        }

        /// <summary>
        /// Property returning a unique static GUID for the step.
        /// </summary>
        public Guid UniqueID
        {
            get { return MY_ID; }
        }
        static readonly Guid MY_ID = new Guid("{89f277e6-42df-4795-8613-277e33cef5d4}");

        /// <summary>
        /// Property returning the number of exits out of the step. Can return either 1 or 2.
        /// </summary>
        public int NumberOfExits
        {
            get { return 1; }
        }

        /// <summary>
        /// Method called that defines the property schema for the step.
        /// </summary>
        public void DefineSchema(IPropertyDefinitions schema)
        {
            // Example of how to add a property definition to the step.
            IPropertyDefinition pd;
            pd = schema.AddStringProperty("StepLocation", "??");
            pd.DisplayName = "Step's Location";
            pd.Description = "The Step's location, e.g. On entering.";
            pd.Required = true;

        }

        /// <summary>
        /// Method called to create a new instance of this step type to place in a process.
        /// Returns an instance of the class implementing the IStep interface.
        /// </summary>
        public IStep CreateStep(IPropertyReaders properties)
        {
            return new EntityWithDataStep(properties);
        }

        #endregion
    }

    /// <summary>
    /// Our Custom Step.
    /// </summary>
    class EntityWithDataStep : IStep
    {
        IPropertyReaders _properties;


        public EntityWithDataStep(IPropertyReaders properties)
        {
            _properties = properties;
        }

        #region IStep Members

        /// <summary>
        /// Method called when a process token executes the step.
        /// </summary>
        public ExitType Execute(IStepExecutionContext context)
        {
            // Examine what our AssociatedObject is
            var simioEntity = context.AssociatedObject;

            string name = simioEntity.HierarchicalDisplayName;

            var info = context.ExecutionInformation;
            string runInfo = $"Model={info.ModelName} Experiment={info.ExperimentName} Scenario={info.ScenarioName} Rep#={info.ReplicationNumber}";
            string uniqueKey = $"{info.ModelName}:{info.ExperimentName}:{info.ScenarioName}:{info.ReplicationNumber}";

            // Example of how to get the value of a step property, in this case where the Step is located,
            // which is set by the human when the step is placed.
            IPropertyReader myExpressionProp = _properties.GetProperty("StepLocation") as IPropertyReader;
            string myLocation = myExpressionProp.GetStringValue(context);

            if (name.StartsWith("DefaultEntity."))
            {
                string[] tokens = name.Split('.');

                DataShareSingleton dss = DataShareSingleton.Instance;

                string key = name;
                MyEntityData entityData = null;

                // Fetch or create our entity data that will accompany the entity.
                // This data is stored in a dictionary within singleton object.
                // and fetched by its key (the entity name)
                object obj = null;
                if (!dss.RuntimeInfoDict.TryGetValue(key, out obj))
                {
                    entityData = new MyEntityData(key);
                    dss.RuntimeInfoDict.TryAdd(key, entityData);
                }
                else
                {
                    entityData = obj as MyEntityData;
                }

                var whereAmI = myLocation;

                entityData.TheStepsIHaveSeen.Add(whereAmI);
                entityData.LastLocationTime = DateTimeOffset.Now;

                // We're only going to do processing for two locations here, but you
                // can see that any of the identified steps can do processing.
                switch (myLocation)
                {
                    case "OnCreated": // Create our data structures
                        {
                            // A one-time build of data
                            if (entityData.DummyList.Count == 0)
                            {
                                entityData.DummyList = new List<DummyDataClass>();

                                // Let's store a bunch of objects for this entity
                                for (int kk = 0; kk < 100000; kk++)
                                {
                                    DummyDataClass ddc = new DummyDataClass(kk, (double)kk * kk);
                                    entityData.DummyList.Add(ddc);
                                }
                            }

                        };
                        break;

                    case "OnVisiting":
                        {

                        }
                        break;
                    
                    case "OnDestroying":
                        {
                            TimeSpan delta = DateTimeOffset.Now.Subtract(entityData.CreationTime);
                            StringBuilder sb = new StringBuilder($" It took me {delta.TotalMilliseconds} ms to visit: ");
                            var writeOptions = ((IStringState)simioEntity.States["WriteOptions"]).Value;

                            foreach (string ss in entityData.TheStepsIHaveSeen)
                                sb.Append($"{ss} ");

                            // Example of how to display a trace line for the step.
                            context.ExecutionInformation.TraceInformation($"{sb}");

                            // Emulate the writing of the data using a very slow protocol
                            if (writeOptions == "Async")
                            {
                                Task task = SendData(dss, entityData);
                            }
                            else if (writeOptions == "Sync")
                            {
                                SendDataSync(dss, entityData);
                            }
                            else // If neither... don't write, but we better release our memory.
                            {
                                ReleaseMemory(dss, entityData);
                            }
                        };
                        break;
                    case "Server1_BeforeProcessing":
                        break;
                    case "Server1_AfterProcessing":
                        break;
                    case "Source_OnCreating":
                        break;

                    default:
                        {
                            context.ExecutionInformation.TraceInformation($"Unhandled StepLocation={myLocation}");
                        };
                        break;
                } // switch

            }
            else // put logic for any unhandled associated objects.
            {
                string xx = "";
            }

            return ExitType.FirstExit;
        }

        /// <summary>
        /// Note: Use this if you want to emulate communicating with external devices.
        /// Emulates a very long-running send operation, followed by a memory release.
        /// Note that we don't care about the status of the 'send', but if we
        /// did, it should likely be logged (since the model has 'moved on' anyway).
        /// </summary>
        /// <param name="myData">Entity's data</param>
        /// <returns></returns>
        private async Task SendData(DataShareSingleton dss, MyEntityData myData)
        {
            await Task.Run(() =>
                {
                    int nn = 0;
                    foreach (DummyDataClass ddc in myData.DummyList)
                    {
                        if ((nn % 700) == 0)
                            System.Threading.Thread.Sleep(10);

                        nn++;
                    }

                    ReleaseMemory(dss, myData);
                });
        }

        // Synchronous case for comparison
        private void SendDataSync(DataShareSingleton dss, MyEntityData myData)
        {
            int nn = 0;
            foreach (DummyDataClass ddc in myData.DummyList)
            {
                if ((nn % 700) == 0)
                    System.Threading.Thread.Sleep(10);

                nn++;
            }

            ReleaseMemory(dss, myData);
        }

        /// <summary>
        /// Explicitly clear the list (which isn't technically required, since the removal of
        /// our object will cause the GCC (the .NET garbage collector) to remove it).
        /// The GCC has overhead, so in a real application you may wish to re-use these data
        /// objects instead of destroying them.
        /// </summary>
        /// <param name="dss"></param>
        /// <param name="data"></param>
        private void ReleaseMemory(DataShareSingleton dss, MyEntityData data)
        {
            data.DummyList.Clear();

            Object obj = null;
            if (!dss.RuntimeInfoDict.TryRemove(data.Key, out obj))
            {
                // perhaps log error?... it is up to you
                string xx = "";
            }
        }


        #endregion
    }
}
