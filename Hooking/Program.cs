using Swed32;
using System.Runtime.InteropServices;

Swed mauri = new Swed("ac_client");
IntPtr client = mauri.GetModuleBase(".exe");
IntPtr processHandle = OpenProcess(0x0008 | 0x0020, 1, mauri.GetProcess().Id);

// === Allocate code caves ===
IntPtr godmodeCave = CreateCodeCave(processHandle, 100);
IntPtr ammoCave = CreateCodeCave(processHandle, 100);

// === Detour Addresses ===
IntPtr godmodeDetour = IntPtr.Add(client, 0x1C223);   // sub [ebx+4], esi
IntPtr ammoDetour = IntPtr.Add(client, 0xC73EF);      // dec [eax]

// === GODMODE ===
byte[] godmodeCode =
{
    0x01, 0x73, 0x04,     // add [ebx+4], esi
    0x8B, 0xC6            // mov eax, esi
};
mauri.WriteBytes(godmodeCave, godmodeCode);
makeDetour(IntPtr.Add(godmodeCave, 5), IntPtr.Add(godmodeDetour, 5), 5);
makeDetour(godmodeDetour, godmodeCave, 5);

// === INFINITE AMMO ===
byte[] ammoCode =
{
    0xFF, 0x00,           // inc [eax]
    0x8D, 0x44, 0x24, 0x1C // lea eax,[esp+1C]
};
mauri.WriteBytes(ammoCave, ammoCode);
makeDetour(IntPtr.Add(ammoCave, 6), IntPtr.Add(ammoDetour, 6), 5);
makeDetour(ammoDetour, ammoCave, 6);

Console.WriteLine("Godmode and infinite ammo hooked.");
Console.ReadLine();

// === Restore original instructions ===
mauri.WriteBytes(godmodeDetour, "29 73 04 8B C6"); // sub [ebx+4], esi / mov eax, esi
mauri.WriteBytes(ammoDetour, "FF 08 8D 44 24 1C"); // dec [eax], lea eax,[esp+1C]

// Free memory
freeCodeCave(processHandle, godmodeCave);
freeCodeCave(processHandle, ammoCave);
CloseHandle(processHandle);

Console.WriteLine("Restored original instructions and freed caves.");
Console.ReadLine();

#region Win32 + Helpers

[DllImport("kernel32.dll")]
static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, int dwProcessId);

[DllImport("kernel32.dll")]
static extern int CloseHandle(IntPtr hObject);

[DllImport("kernel32.dll")]
static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddres, int dwSize, uint flAllocationType, uint flProtect);

[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, int dwFreeType);

void makeDetour(IntPtr address, IntPtr destination, int bytesToPatch)
{
    int offset = (int)destination - (int)(address + 5);
    byte[] patch = new byte[bytesToPatch];
    for (int i = 0; i < bytesToPatch; i++) patch[i] = 0x90;
    patch[0] = 0xE9;
    BitConverter.GetBytes(offset).CopyTo(patch, 1);
    mauri.WriteBytes(address, patch);
}

IntPtr CreateCodeCave(IntPtr processHandle, int size)
{
    return VirtualAllocEx(processHandle, (IntPtr)null, size, 0x1000 | 0x2000, 0x40);
}

bool freeCodeCave(IntPtr processHandle, IntPtr caveAddress)
{
    return VirtualFreeEx(processHandle, caveAddress, 0, 0x00008000);
}

#endregion
