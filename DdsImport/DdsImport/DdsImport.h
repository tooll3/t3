#pragma once
#include <vcclr.h>
#include "..\DdsTextureLoader11\DDSTextureLoader11.h"

using namespace System;
using namespace DirectX;

namespace T3 
{
    public ref class DdsImport
    {
      public:
        static void CreateDdsTextureFromFile(IntPtr device, IntPtr context, String^ path,
                                             [Runtime::InteropServices::Out] IntPtr% outTexture,
                                             [Runtime::InteropServices::Out] IntPtr% outSrv)
        {
            ID3D11Device* device2 = (ID3D11Device*)device.ToPointer();
            ID3D11DeviceContext* context2 = (ID3D11DeviceContext*)context.ToPointer();
            ID3D11ShaderResourceView* srv;
            ID3D11Resource* texture;
            pin_ptr<const wchar_t> pinnedPath = PtrToStringChars(path);
            HRESULT res = CreateDDSTextureFromFile(device2, context2, pinnedPath, &texture, &srv);
            outTexture = IntPtr(texture);
            outSrv = IntPtr(srv);
        }
    };
}

