using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Core_DI_analysis
{
    public static class Analyzer
    {
        public static ServiceTree CheckDependendencies(IServiceCollection serviceCollection, Func<ServiceDescriptor, bool> where)
        {
            var services = CreateServices(serviceCollection, where);

            return new ServiceTree(services);
        }

        private static ICollection<Service> CreateServices(IServiceCollection serviceCollection, Func<ServiceDescriptor, bool> where)
        {
            return serviceCollection
                .Where(where)
                .Select(o => new Service(o))
                .ToList();
        }
    }

    public class ServiceTree
    {
        private ICollection<Service> Services { get; set; }

        public ServiceTree(ICollection<Service> services)
        {
            Services = services;
            ConnectChildren();
        }

        private void ConnectChildren()
        {
            foreach (var service in Services.Where(o => o.ImplementationType != null))
            {
                var parameterTypeNames = service.ImplementationType.GetConstructors()
                    .Where(o => o.GetParameters().Any())
                    .SelectMany(o => o.GetParameters()
                        .Select(parameter => parameter.ParameterType.Name.ToString()));

                var children = Services.Where(o => parameterTypeNames.Contains(o.Name)).ToList();
                service.AddChildren(children);
            }
        }

        public void PrintListByTotalChildCount()
        {
            Console.WriteLine("---------------Services---------------");
            Services
            .OrderByDescending(o => o.GetTotalChildCount())
            .ToList()
            .ForEach(o => o.PresentTotalChildCount());
            Console.WriteLine("--------------------------------------");
        }
    }

    public class Service
    {
        public string Name { get => ServiceType?.Name?.ToString(); }

        public Type ServiceType { get; }
        public Type ImplementationType { get; }

        public IEnumerable<Service> Children { get; private set; }

        public Service(ServiceDescriptor descriptor)
        {
            ServiceType = descriptor.ServiceType;
            ImplementationType = descriptor.ImplementationType;
            Children = new List<Service>();
        }

        public void AddChildren(ICollection<Service> children)
        {
            Children = Children.Concat(children);
        }

        public int GetTotalChildCount()
        {
            var ownChildCount = Children.Count();
            var grandChildCount = Children.Sum(o => o.GetTotalChildCount());

            return ownChildCount + grandChildCount;
        }

        public void PresentTotalChildCount()
        {
            Console.WriteLine($"{Name}: {GetTotalChildCount()} children");
        }
    }
}
