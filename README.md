# gordon's Windows SBS pop3connector Password Recovery

This tool is meant to be used by server administrators to recover lost or undocumented E-Mail passwords on a Microsoft Small Business Server.
This tool reads the file `pop3records.dat` created by the POP3 connector. Passwords are encrypted **machine dependent**, so it is required to **run this tool directly on the server**.
Decryption was tested successfully on **Microsoft Small Business Server version 2008 and 2011**


## The interesting part
... is nothing more than this:

```C#
string decryptPass(string ciphertext, byte[] salt)
{
    return Encoding.Unicode.GetString(
        ProtectedData.Unprotect(Convert.FromBase64String(ciphertext), salt, DataProtectionScope.LocalMachine)
    );
}
```

In this code snippet you can see that the "magic" was done by some built in .net functions. The salt is stored in `pop3records.dat` too and is called `entropy` in this file. The salt has to be Base64 decoded like the ciphertext.
