using System;

namespace TrustEXE.Acceso
{
    [Serializable]
    public sealed class AccesoState
    {
        public bool card, linked, powered, decoded, verified, doorB, completed;
        public float pulseP1 = -100, pulseP2 = -100;
        public bool DoorA => card && linked;
        public bool CanSync => DoorA && powered && decoded && verified;
        public bool Code(int a, int b, int c) { if (!DoorA || a != 3 || b != 1 || c != 2) return false; decoded = true; return true; }
        public bool Verify(bool reportsPower) { if (!powered || !decoded || !reportsPower) return false; verified = true; return true; }
        public bool Pulse(int player, float now)
        {
            if (!CanSync || doorB) return false;
            if (player == 0) pulseP1 = now; else pulseP2 = now;
            doorB = Math.Abs(pulseP1 - pulseP2) <= 6 && now - Math.Min(pulseP1, pulseP2) <= 6;
            return doorB;
        }
        public void ResetTimers() { pulseP1 = pulseP2 = -100; }
        public string Objective(int p)
        {
            if (!DoorA) return p == 0 ? (card ? "Espera el enlace de P2 · puerta A" : "Recoge la tarjeta en RECEPCIÓN") : (linked ? "Espera la tarjeta de P1 · puerta A" : "Activa el nodo 01 en RECEPCIÓN");
            if (!powered || !decoded) return p == 0 ? (powered ? "Comparte el código 3 · 1 · 2 y la lectura 120 V" : "Activa el suministro en la sala derecha") : (decoded ? "Pide a P1 la lectura real del suministro" : "Introduce la clave de P1 en el nodo 02");
            if (!verified) return p == 0 ? "Comunica la lectura física: 120 V / ENCENDIDO" : "Contrasta a E.V.A. en el diagnóstico del nodo 02";
            if (!doorB) return "Activen ambos controles SYNC con menos de 6 s de diferencia";
            return "Cruza la puerta B · llega a SALIDA con tu compañero";
        }
    }
}
