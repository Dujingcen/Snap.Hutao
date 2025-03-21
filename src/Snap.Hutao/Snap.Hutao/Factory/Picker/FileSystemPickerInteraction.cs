// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Core.IO;
using Snap.Hutao.Core.LifeCycle;
using Snap.Hutao.Win32.Foundation;
using Snap.Hutao.Win32.System.Com;
using Snap.Hutao.Win32.UI.Shell;
using Snap.Hutao.Win32.UI.Shell.Common;
using System.Runtime.InteropServices;
using WinRT;
using static Snap.Hutao.Win32.Macros;
using static Snap.Hutao.Win32.Ole32;
using static Snap.Hutao.Win32.Shell32;

namespace Snap.Hutao.Factory.Picker;

[ConstructorGenerated]
[Injection(InjectAs.Transient, typeof(IFileSystemPickerInteraction))]
internal sealed partial class FileSystemPickerInteraction : IFileSystemPickerInteraction
{
    private readonly ICurrentXamlWindowReference currentWindowReference;

    public unsafe ValueResult<bool, ValueFile> PickFile(string? title, string? defaultFileName, (string Name, string Type)[]? filters)
    {
        HRESULT hr = CoCreateInstance(in FileOpenDialog.CLSID, default, CLSCTX.CLSCTX_INPROC_SERVER, in IFileDialog.IID, out ObjectReference<IFileDialog.Vftbl> fileDialog);
        Marshal.ThrowExceptionForHR(hr);

        using (fileDialog)
        {
            const FILEOPENDIALOGOPTIONS Options =
                FILEOPENDIALOGOPTIONS.FOS_NOTESTFILECREATE |
                FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM |
                FILEOPENDIALOGOPTIONS.FOS_NOCHANGEDIR;

            fileDialog.GetOptions(out FILEOPENDIALOGOPTIONS original);
            fileDialog.SetOptions(original | Options);
            SetDesktopAsStartupFolder(fileDialog);

            if (!string.IsNullOrEmpty(defaultFileName))
            {
                fileDialog.SetFileName(defaultFileName);
            }

            if (!string.IsNullOrEmpty(title))
            {
                fileDialog.SetTitle(title);
            }

            if (filters is { Length: > 0 })
            {
                SetFileTypes(fileDialog, filters);
            }

            HRESULT res = fileDialog.Show(currentWindowReference.GetWindowHandle());
            if (res == HRESULT_FROM_WIN32(WIN32_ERROR.ERROR_CANCELLED))
            {
                return new(false, default);
            }

            Marshal.ThrowExceptionForHR(res);

            _ = fileDialog.GetResult(out ObjectReference<IShellItem.Vftbl> shellItem);

            using (shellItem)
            {
                PWSTR displayName = default;
                string file;
                try
                {
                    shellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out displayName);
                    file = new(displayName);
                }
                finally
                {
                    CoTaskMemFree(displayName);
                }

                return new(true, file);
            }
        }
    }

    public unsafe ValueResult<bool, ValueFile> SaveFile(string? title, string? defaultFileName, (string Name, string Type)[]? filters)
    {
        HRESULT hr = CoCreateInstance(in FileSaveDialog.CLSID, default, CLSCTX.CLSCTX_INPROC_SERVER, in IFileDialog.IID, out ObjectReference<IFileDialog.Vftbl> fileDialog);
        Marshal.ThrowExceptionForHR(hr);

        using (fileDialog)
        {
            const FILEOPENDIALOGOPTIONS Options =
                FILEOPENDIALOGOPTIONS.FOS_OVERWRITEPROMPT |
                FILEOPENDIALOGOPTIONS.FOS_NOTESTFILECREATE |
                FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM |
                FILEOPENDIALOGOPTIONS.FOS_STRICTFILETYPES |
                FILEOPENDIALOGOPTIONS.FOS_NOCHANGEDIR;

            fileDialog.GetOptions(out FILEOPENDIALOGOPTIONS original);
            fileDialog.SetOptions(original | Options);
            SetDesktopAsStartupFolder(fileDialog);

            if (!string.IsNullOrEmpty(defaultFileName))
            {
                fileDialog.SetFileName(defaultFileName);
            }

            if (!string.IsNullOrEmpty(title))
            {
                fileDialog.SetTitle(title);
            }

            if (filters is { Length: > 0 })
            {
                SetFileTypes(fileDialog, filters);
            }

            HRESULT res = fileDialog.Show(currentWindowReference.GetWindowHandle());
            if (res == HRESULT_FROM_WIN32(WIN32_ERROR.ERROR_CANCELLED))
            {
                return new(false, default);
            }

            Marshal.ThrowExceptionForHR(res);

            fileDialog.GetResult(out ObjectReference<IShellItem.Vftbl> shellItem);

            using (shellItem)
            {
                PWSTR displayName = default;
                string file;
                try
                {
                    shellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out displayName);
                    file = new(displayName);
                }
                finally
                {
                    CoTaskMemFree(displayName);
                }

                return new(true, file);
            }
        }
    }

    public unsafe ValueResult<bool, string> PickFolder(string? title)
    {
        HRESULT hr = CoCreateInstance(in FileOpenDialog.CLSID, default, CLSCTX.CLSCTX_INPROC_SERVER, in IFileDialog.IID, out ObjectReference<IFileDialog.Vftbl> fileDialog);
        Marshal.ThrowExceptionForHR(hr);

        using (fileDialog)
        {
            const FILEOPENDIALOGOPTIONS Options =
                FILEOPENDIALOGOPTIONS.FOS_NOTESTFILECREATE |
                FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM |
                FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS |
                FILEOPENDIALOGOPTIONS.FOS_NOCHANGEDIR;

            fileDialog.GetOptions(out FILEOPENDIALOGOPTIONS original);
            fileDialog.SetOptions(original | Options);
            SetDesktopAsStartupFolder(fileDialog);

            if (!string.IsNullOrEmpty(title))
            {
                fileDialog.SetTitle(title);
            }

            HRESULT res = fileDialog.Show(currentWindowReference.GetWindowHandle());
            if (res == HRESULT_FROM_WIN32(WIN32_ERROR.ERROR_CANCELLED))
            {
                return new(false, default!);
            }

            Marshal.ThrowExceptionForHR(res);

            fileDialog.GetResult(out ObjectReference<IShellItem.Vftbl> shellItem);

            using (shellItem)
            {
                PWSTR displayName = default;
                string file;
                try
                {
                    shellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out displayName);
                    file = new(displayName);
                }
                finally
                {
                    CoTaskMemFree(displayName);
                }

                return new(true, file);
            }
        }
    }

    private static unsafe void SetFileTypes(ObjectReference<IFileDialog.Vftbl> fileDialog, ReadOnlySpan<(string Name, string Type)> filters)
    {
        Span<nint> unmanagedStringPtrs = filters.Length < 16 ? stackalloc nint[filters.Length * 2] : new nint[filters.Length * 2];
        Span<COMDLG_FILTERSPEC> filterSpecs = stackalloc COMDLG_FILTERSPEC[filters.Length];

        for (int index = 0; index < filters.Length; index++)
        {
            (string name, string type) = filters[index];
            nint pName = Marshal.StringToCoTaskMemUni(name);
            nint pType = Marshal.StringToCoTaskMemUni(type);
            unmanagedStringPtrs[index * 2] = pName;
            unmanagedStringPtrs[(index * 2) + 1] = pType;
            COMDLG_FILTERSPEC spec = default;
            spec.pszName = *(PCWSTR*)&pName;
            spec.pszSpec = *(PCWSTR*)&pType;
            filterSpecs[index] = spec;
        }

        fileDialog.SetFileTypes(filterSpecs);

        foreach (ref readonly nint ptr in unmanagedStringPtrs)
        {
            CoTaskMemFree((void*)ptr);
        }
    }

    private static void SetDesktopAsStartupFolder(ObjectReference<IFileDialog.Vftbl> fileDialog)
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        HRESULT hr = SHCreateItemFromParsingName(desktopPath, default, in IShellItem.IID, out ObjectReference<IShellItem.Vftbl> shellItem);
        Marshal.ThrowExceptionForHR(hr);

        using (shellItem)
        {
            fileDialog.SetFolder(shellItem);
        }
    }
}