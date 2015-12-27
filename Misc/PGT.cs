﻿using System;

namespace PKHeX
{
    public class PCD
    {
        internal static int Size = 0x358; // 856

        public byte[] Data;
        public PCD(byte[] data = null)
        {
            Data = data ?? new byte[Size];

            byte[] giftData = new byte[PGT.Size];
            Array.Copy(Data, 0, giftData, 0, PGT.Size);
            Gift = new PGT(giftData);

            Information = new byte[Data.Length - PGT.Size];
            Array.Copy(Data, PGT.Size, Information, 0, Information.Length);
        }
        public PGT Gift;

        public byte[] Information;
        /* Big thanks to Grovyle91's Pokémon Mystery Gift Editor, from which the structure was documented.
         * http://projectpokemon.org/forums/member.php?829-Grovyle91
         * http://projectpokemon.org/forums/showthread.php?6524
         * Offsets are relative to the entire PCD
         * [Flags]
         * public enum RedistributionGame
         * {
         *     None = 0,
         *     Diamond = 4,
         *     Pearl = 8,
         *     Platinum = 16,
         *     HeartGold = 32768, // highest bit
         *     SoulSilver = 1, // lowest bit
         * }
         * private const int CardTitle = 260;
         * private const int CardTitle_Length = 72;
         * private const int CardComment = 340;
         * private const int CardComment_Length = 492;
         * private const int WonderCardNumber = 336;
         * private const int RedistributionGame = 332;
         * private const int RedistributionAmount = 840;
         * private const int Icon1 = 842;
         * private const int Icon2 = 844;
         * private const int Icon3 = 846;
         * private const int ReceivedDate = 852;
         */
    }
    public class PGT
    {
        internal static int Size = 0x104; // 260

        public byte[] Data;
        public PGT(byte[] data = null)
        {
            Data = data ?? new byte[Size];
            byte[] ekdata = new byte[PK4.SIZE_PARTY];
            Array.Copy(Data, 8, ekdata, 0, ekdata.Length);
            // Decrypt PK4
            PKM = new PK4(PKX.decryptG4Array(ekdata, BitConverter.ToUInt16(ekdata, 6)));
            
            Unknown = new byte[0x10];
            Array.Copy(Data, 0xF4, Unknown, 0, 0x10);
        }

        public byte CardType { get { return Data[0]; } set { Data[0] = value; } }
        // Unused 0x01
        public byte Slot { get { return Data[2]; } set { Data[2] = value; } }
        public byte Detail { get { return Data[3]; } set { Data[3] = value; } }
        public PK4 PKM;
        public byte[] Unknown;

        public bool IsPokémon { get { return CardType == 1; } set { if (value) CardType = 1; } }
        public bool IsEgg { get { return CardType == 2; } set { if (value) CardType = 2; } }
        public bool IsManaphyEgg { get { return CardType == 7; } set { if (value) CardType = 7; } }
        public bool Pokémon { get { return IsPokémon || IsEgg || IsManaphyEgg; } }

        public PK4 convertToPK4(SAV6 SAV)
        {
            if (!Pokémon)
                return null;

            if (CardType > 1 && Detail == 0)
            {
                PKM.OT_Name = "\x013A\x0135\x0132\x0149\x0142"; // PKHeX
                PKM.TID = 12345;
                PKM.SID = 54321;
                PKM.OT_Gender = (int)(Util.rnd32()%2);
            }

            // Generate IV
            uint seed = Util.rnd32();
            if (PKM.PID == 1) // Create Nonshiny
            {
                uint pid1 = PKX.LCRNG(ref seed) >> 16;
                uint pid2 = PKX.LCRNG(ref seed) >> 16;

                while ((pid1 ^ pid2 ^ PKM.TID ^ PKM.SID) < 8)
                {
                    uint testPID = pid1 | (pid2 << 16);

                    // Call the ARNG to change the PID
                    testPID = testPID * 0x6c078965 + 1;

                    pid1 = testPID & 0xFFFF;
                    pid2 = testPID >> 16;
                }
                PKM.PID = pid1 | (pid2 << 16);
            }

            // Generate IVs
            if (PKM.IV32 == 0)
            {
                uint iv1 = PKX.LCRNG(ref seed) >> 16;
                uint iv2 = PKX.LCRNG(ref seed) >> 16;
                PKM.IV32 = (iv1 | (iv2 << 16)) & 0x3FFFFFFF;
            }

            // Generate Met Info
            DateTime dt = DateTime.Now;
            if (IsPokémon)
            {
                PKM.Met_Location = PKM.Egg_Location + 3000;
                PKM.Egg_Location = 0;
                PKM.Met_Day = dt.Day;
                PKM.Met_Month = dt.Month;
                PKM.Met_Year = dt.Year - 2000;
                PKM.IsEgg = false;
            }
            else
            {
                PKM.Egg_Location = PKM.Egg_Location + 3000;
                PKM.Egg_Day = dt.Day;
                PKM.Egg_Month = dt.Month;
                PKM.Egg_Year = dt.Year - 2000;
                PKM.IsEgg = false;
                // Met Location is modified when transferred to PK5; don't worry about it.
            }

            return PKM;
        }
    }
}