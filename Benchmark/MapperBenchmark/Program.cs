﻿namespace MapperBenchmark
{
    using AutoMapper;

    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Running;

    public static class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<Benchmark>();
        }
    }

    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            Add(MarkdownExporter.Default, MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
            Add(Job.ShortRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark
    {
        private readonly SimpleSource simpleSource = new SimpleSource();

        private IMapper mapper;

        [GlobalSetup]
        public void Setup()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<SimpleSource, SimpleDestination>();
            });

            mapper = config.CreateMapper();
        }

        [Benchmark]
        public SimpleDestination MapSimple()
        {
            return mapper.Map<SimpleSource, SimpleDestination>(simpleSource);
        }
    }

    public class SimpleSource
    {
        public int Value { get; set; }
    }

    public class SimpleDestination
    {
        public int Value { get; set; }
    }
}