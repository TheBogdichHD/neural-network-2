using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeuralNetwork1;
using System.Text.Json;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Accord.WindowsForms
{
    class SampleController
    {
        public SamplesSet sampleSet { get; private set; }
        public int Size { get { return sampleSet.samples.Count; } }
        public SampleController()
        {
            sampleSet = new SamplesSet();
        }

        public void AddSample(Sample s)
        {
            sampleSet.AddSample(s);
        }
        public void Clear()
        {
            sampleSet = new SamplesSet();
        }
        public void Save(string path)
        {
            var bf = new BinaryFormatter();
            var fs = new FileStream(path, FileMode.Create);
            bf.Serialize(fs, sampleSet);
        }
        public void Load(string path)
        {
            var bf = new BinaryFormatter();
            var fs = new FileStream(path, FileMode.Open);
            var new_ampleSet = (SamplesSet)bf.Deserialize(fs);
            foreach (Sample sample in new_ampleSet)
            {
                sampleSet.AddSample(sample);
            }
        }
        public void Shuffle()
        {
            sampleSet.Shuffle();
        }

    }
}
