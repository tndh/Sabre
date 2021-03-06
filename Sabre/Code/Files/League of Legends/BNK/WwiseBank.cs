﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Sabre
{
    public class WwiseBank
    {
        public List<BankSection> bankSections = new List<BankSection>();

        public WwiseBank(string fileLocation)
        {
            using (BinaryReader br = new BinaryReader(File.Open(fileLocation, FileMode.Open)))
            {
                this.Read(br);
            }
        }

        public void Save(string fileLocation)
        {
            using (BinaryWriter bw = new BinaryWriter(File.Open(fileLocation, FileMode.Create)))
            {
                this.Write(bw);
            }
        }

        private void Read(BinaryReader br)
        {
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                string sectionName = Encoding.ASCII.GetString(br.ReadBytes(4));
                uint sectionLength = br.ReadUInt32();
                switch (sectionName)
                {
                    case "BKHD":
                        this.bankSections.Add(new BKHDSection(br, sectionLength));
                        break;
                    case "HIRC":
                        this.bankSections.Add(new HIRCSection(br));
                        break;
                    case "STID":
                        this.bankSections.Add(new STIDSection(br));
                        break;
                    case "DIDX":
                        this.bankSections.Add(new DIDXSection(br, sectionLength));
                        break;
                    case "DATA":
                        this.bankSections.Add(new DATASection(br, sectionLength, (DIDXSection)this.GetSection("DIDX")));
                        break;
                    default:
                        this.bankSections.Add(new BankSection(sectionName, br.ReadBytes((int)sectionLength)));
                        break;
                }
            }
        }

        public BankSection GetSection(string sectionName)
        {
            return this.bankSections.Find(x => x.sectionName == sectionName);
        }

        private void Write(BinaryWriter bw)
        {
            foreach (BankSection bnkSection in this.bankSections)
            {
                bw.Write(bnkSection.GetBytes());
            }
        }
    }
}
