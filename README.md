# OpenNP

Ứng dụng overlay bàn phím số Windows nhẹ (.NET 8 WPF), luôn trên cùng, phản hồi phím realtime.

## Tính năng
- Overlay Topmost trong suốt
- Bắt phím numpad toàn cục (hook)
- Hiển thị trạng thái NumLock
- Tùy chỉnh: độ mờ, kích thước (nhỏ/trung/lớn), phím tắt
- Menu khay hệ thống + lưu settings.json

## Chạy nhanh
```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
./publish/OpenNP.exe
```

Phím tắt mặc định: `Ctrl + ``

## Cài đặt
Mở bằng nút ⚙ hoặc menu khay → chỉnh opacity, size, hotkey.

## Xây dựng
Yêu cầu: .NET 8 SDK + Windows.

## Giấy phép
MIT

---
Repo: https://github.com/longhangoc/numpadcsharp