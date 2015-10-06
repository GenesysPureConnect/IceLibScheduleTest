using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ININ.IceLib.Configuration;
using ININ.IceLib.Connection;

namespace ScheduleTest
{
    class Program
    {
        private static Session _session = new Session();
        private static ScheduleConfigurationList _scheduleConfigurationList;

        static void Main(string[] args)
        {
            try
            {
                var host = GetConsoleInput("CIC server name:");
                var user = GetConsoleInput("CIC user name:");
                var password = GetConsoleInput("CIC password:");
                
                Console.WriteLine("Connecting....");
                _session.Connect(new SessionSettings(), new HostSettings(new HostEndpoint(host)),
                    new ICAuthSettings(user, password), new StationlessSettings());

                Console.WriteLine("Connected to {0} as {1}", _session.GetHostSettings().HostEndpoint.Host, _session.UserId);

                _scheduleConfigurationList = new ScheduleConfigurationList(ConfigurationManager.GetInstance(_session));
                
                GetSchedules();

                _session.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }

        private static void GetSchedules()
        {
            try
            {
                var quit = false;
                while (!quit)
                {
                    try
                    {
                        if (_scheduleConfigurationList.IsCaching) _scheduleConfigurationList.StopCaching();

                        var name = GetConsoleInput("Enter schedule name or \"quit\" to exit:");

                        if (string.IsNullOrEmpty(name)) continue;
                        if (name.Equals("quit", StringComparison.InvariantCultureIgnoreCase)) return;

                        var query = _scheduleConfigurationList.CreateQuerySettings();
                        query.SetRightsFilterToAdmin();
                        query.SetPropertiesToRetrieveToAll();
                        query.SetFilterDefinition(ScheduleConfiguration.Property.Id, name);

                        var recurranceQuery = _scheduleConfigurationList.CreateRecurrenceQuerySettings();
                        var queryChildren = new ScheduleQueryChildrenSettings();
                        recurranceQuery.SetPropertiesToRetrieveToAll();
                        queryChildren.ScheduleRecurrence = recurranceQuery;
                        query.SetChildQuerySettings(queryChildren);

                        _scheduleConfigurationList.StartCaching(query);

                        var list = _scheduleConfigurationList.GetConfigurationList();

                        Console.WriteLine("Got {0} schedules", list.Count);

                        ScheduleConfiguration schedule;

                        if (list.Count > 1)
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                var s = list[i];
                                Console.WriteLine("Press {0} for {1}", i, s.ConfigurationId.DisplayName);
                            }
                            var choice = Console.ReadLine();

                            int key = 0;
                            if (!int.TryParse(choice,out key)) continue;

                            schedule = list[key];
                        }
                        else if (list.Count == 0)
                        {
                            continue;
                        }
                        else
                        {
                            schedule = list[0];
                        }

                        Console.WriteLine("Schedule active={0}. Press 1 to set active, 2 to set inactive.", schedule.IsActive.Value);
                        var active = Console.ReadLine();
                        
                        schedule.PrepareForEdit();
                        schedule.IsActive.Value = active.Equals("1", StringComparison.InvariantCultureIgnoreCase);
                        schedule.Commit();

                        _scheduleConfigurationList.StopCaching();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        Console.ReadLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }

        private static string GetConsoleInput(string prompt, params string[] values)
        {
            Console.Write(prompt.Trim() + " ", values);
            return Console.ReadLine();
        }
    }
}
