using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using log4net;
using SchedulerManager.Log4Net;

namespace SchedulerManager.Mechanism
{
    /// <summary>
    /// Job mechanism manager.
    /// </summary>
    public class JobManager
    {
        private ILog log = LogManager.GetLogger(Log4NetConstants.SCHEDULER_LOGGER);

        /// <summary>
        /// Execute all Jobs.
        /// </summary>
        public void ExecuteAllJobs()
        {
            log.Debug("Begin Method");

            try
            {
                // get all job implementations of this assembly.
                IEnumerable<Type> jobs = GetAllTypesImplementingInterface(typeof(Job));
                // execute each job
                if (jobs != null && jobs.Count() > 0)
                {
                    Job instanceJob = null;
                    Thread thread = null;
                    foreach (Type job in jobs)
                    {
                        // only instantiate the job its implementation is "real"
                        if (IsRealClass(job))
                        {
                            try
                            {
                                // instantiate job by reflection
                                instanceJob = (Job)Activator.CreateInstance(job);
                                log.Debug(String.Format("The Job \"{0}\" has been instantiated successfully.", instanceJob.GetName()));
                                // create thread for this job execution method
                                thread = new Thread(new ThreadStart(instanceJob.ExecuteJob));
                                // start thread executing the job
                                thread.Start();
                                log.Debug(String.Format("The Job \"{0}\" has its thread started successfully.", instanceJob.GetName()));
                            }
                            catch (Exception ex)
                            {
                                log.Error(String.Format("The Job \"{0}\" could not be instantiated or executed.", job.Name), ex);
                            }
                        }
                        else
                        {
                            log.Error(String.Format("The Job \"{0}\" cannot be instantiated.", job.FullName));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("An error has occured while instantiating or executing Jobs for the Scheduler Framework.", ex);
            }

            log.Debug("End Method");
        }

        /// <summary>
        /// Returns all types in the current AppDomain implementing the interface or inheriting the type. 
        /// </summary>
        private IEnumerable<Type> GetAllTypesImplementingInterface(Type desiredType)
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => desiredType.IsAssignableFrom(type));

        }

        /// <summary>
        /// Determine whether the object is real - non-abstract, non-generic-needed, non-interface class.
        /// </summary>
        /// <param name="testType">Type to be verified.</param>
        /// <returns>True in case the class is real, false otherwise.</returns>
        public static bool IsRealClass(Type testType)
        {
            return testType.IsAbstract == false
                && testType.IsGenericTypeDefinition == false
                && testType.IsInterface == false;
        }
    }
}