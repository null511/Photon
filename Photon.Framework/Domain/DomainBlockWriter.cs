﻿using Photon.Framework.Server;
using System;

namespace Photon.Framework.Domain
{
    [Serializable]
    public class DomainBlockWriter : IBlockWriter<DomainBlockWriter>
    {
        private readonly DomainOutput domainOutput;
        private readonly ScriptOutput output;
        private volatile bool isPosted;


        public DomainBlockWriter(DomainOutput output)
        {
            this.domainOutput = output;

            this.output = new ScriptOutput();
        }

        public void Dispose()
        {
            Post();

            output?.Dispose();
        }

        public DomainBlockWriter Write(string text, ConsoleColor color)
        {
            output.Write(text, color);
            return this;
        }

        public DomainBlockWriter Write(object value, ConsoleColor color)
        {
            output.Write(value, color);
            return this;
        }

        public DomainBlockWriter WriteLine(string text, ConsoleColor color)
        {
            output.WriteLine(text, color);
            return this;
        }

        public DomainBlockWriter WriteLine(object value, ConsoleColor color)
        {
            output.WriteLine(value, color);
            return this;
        }

        public void Post()
        {
            if (isPosted) return;
            isPosted = true;

            domainOutput.WriteRaw(output.GetString());
        }

        void IWrite.Write(string text, ConsoleColor color) => Write(text, color);
        void IWrite.Write(object value, ConsoleColor color) => Write(value, color);
        void IWrite.WriteLine(string text, ConsoleColor color) => WriteLine(text, color);
        void IWrite.WriteLine(object value, ConsoleColor color) => WriteLine(value, color);
    }
}
