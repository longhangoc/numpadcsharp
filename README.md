# OpenNP

OpenNP là ứng dụng overlay bàn phím số Windows nhẹ, luôn hiển thị trên cùng, được xây dựng bằng C# và WPF (.NET 8). Hiển thị bàn phím số ảo trên màn hình và phản hồi phím nhấn theo thời gian thực.

## Tính năng

- **Overlay luôn ở trên cùng**: cửa sổ bàn phím số trong suốt luôn hiển thị phía trước các ứng dụng khác
- **Phản hồi phím thời gian thực**: phím số được bôi sáng khi nhấn
- **Hook phím toàn cục**: bắt và hiển thị mọi phím numpad trên hệ thống
- **Hiển thị trạng thái NumLock**
- **Cài đặt tùy chỉnh**:
  - điều chỉnh độ mờ overlay (10% - 100%)
  - chọn kích thước Nhỏ, Trung bình, hoặc Lớn
  - chọn phím tắt bật/tắt overlay
- **Tích hợp khay hệ thống**: ẩn/hiện overlay, mở cài đặt nhanh, thoát
- **Lưu cấu hình tự động**: cài đặt được lưu vào `settings.json`
- **Xuất file đơn**: có thể publish thành file `.exe` duy nhất

## Yêu cầu hệ thống

- **Hệ điều hành**: Windows 10/11 (64-bit)
- **.NET Runtime**: .NET 8.0+ (nếu không xuất self-contained thì cần cài .NET 8)
- **Kiến trúc**: x64 (win-x64)

## Cài đặt

### Tùy chọn 1: Download bản Release
1. Vào trang Releases của repo
2. Tải file `OpenNP.exe`
3. Chạy file

### Tùy chọn 2: Build từ mã nguồn
```bash
git clone https://github.com/longhangoc/numpadcsharp.git
cd numpadcsharp

dotnet publish NumpadOverlay.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish

# Chạy
./publish/OpenNP.exe
```

## Hướng dẫn sử dụng

1. Chạy file `OpenNP.exe` từ thư mục `publish` hoặc từ nơi bạn đã cài.
2. Overlay OpenNP sẽ xuất hiện trên màn hình với bàn phím số trực quan.
3. Nhấn các phím trên numpad hoặc bàn phím chung để xem hiệu ứng phím đổi màu.
4. Để ẩn/hiện overlay nhanh, nhấn tổ hợp phím `Ctrl + `` (backtick)`, hoặc dùng menu khay hệ thống.
5. Nhấp chuột phải vào icon OpenNP ở khay hệ thống để mở menu nhanh:
   - `Ẩn overlay`: ẩn bảng điều khiển nhưng app vẫn chạy
   - `Hiện overlay`: hiển thị lại nếu đã ẩn
   - `Cài đặt`: mở cửa sổ cài đặt
   - `Thoát`: đóng ứng dụng hoàn toàn
6. Nếu overlay bị ẩn, bạn vẫn có thể mở lại bằng phím tắt hoặc chuột phải vào icon khay.
7. Thông tin NumLock hiển thị ở đầu overlay, giúp bạn biết trạng thái bàn phím số hiện tại.

## Cài đặt

Truy cập cài đặt bằng cách bấm nút ⚙ trên overlay hoặc chọn "Cài đặt" trong menu khay hệ thống.

- **Độ mờ**: điều chỉnh độ trong suốt của overlay
- **Kích thước**: chọn Nhỏ (75%), Trung bình (100%), Lớn (150%)
- **Phím tắt**: chọn phím `Ctrl +` (F1-F12 hoặc phím `)

Mọi thay đổi sẽ được lưu tự động khi đóng cửa sổ cài đặt.

## Cấu trúc dự án

### Thành phần chính

- **MainWindow.xaml / MainWindow.xaml.cs**: giao diện overlay và hook bàn phím
- **SettingsWindow.xaml / SettingsWindow.xaml.cs**: giao diện cài đặt
- **Models/AppSettings.cs**: mô hình cài đặt và lưu JSON
- **App.xaml / App.xaml.cs**: điểm khởi tạo WPF

### Công nghệ chính

- **WPF** cho giao diện người dùng
- **Windows Forms** cho khay hệ thống
- **Low-level keyboard hook** (`WH_KEYBOARD_LL`) để bắt phím toàn cục
- **System.Text.Json** để lưu cài đặt

### Mô hình luồng

- Hook bàn phím chạy trên luồng riêng với apartment STA
- Cập nhật giao diện qua Dispatcher chính để tránh lỗi cross-thread
- Thay đổi cài đặt truyền qua `INotifyPropertyChanged`

## Chi tiết hook phím

Ứng dụng dùng `SetWindowsHookEx` với `WH_KEYBOARD_LL` để chặn toàn bộ phím bàn phím trên hệ thống. Điều này cho phép:
- phát hiện phím numpad ngay cả khi ứng dụng không phải cửa sổ đang hoạt động
- theo dõi không xâm lấn (không ngăn phím đến ứng dụng khác)
- hiển thị phản hồi thời gian thực

## Build

### Yêu cầu

- .NET 8.0 SDK
- Visual Studio 2022 hoặc bất kỳ IDE nào hỗ trợ .NET 8 (không bắt buộc)

### Build Debug

```bash
dotnet build NumpadOverlay.csproj --configuration Debug
```

### Build Release

```bash
dotnet build NumpadOverlay.csproj --configuration Release
```

### Publish file đơn

```bash
dotnet publish NumpadOverlay.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

## File cài đặt

Cài đặt được lưu trong file `settings.json` tại thư mục ứng dụng:

```json
{
  "opacity": 0.85,
  "overlaySize": "medium",
  "toggleHotkeyVirtualKey": 192
}
```

- `opacity`: 0.1 - 1.0 (10% - 100%)
- `overlaySize`: `small`, `medium`, hoặc `large`
- `toggleHotkeyVirtualKey`: mã phím ảo (0xC0 = phím `, 0x70-0x7B = F1-F12)

## Hạn chế

- Chỉ hỗ trợ Windows (dựa vào WinAPI và WPF)
- Có thể cần quyền admin trên một số hệ thống để hook phím toàn cục
- SVG icon không được sử dụng, dùng icon hệ thống thay thế

## Khắc phục sự cố

### Overlay không hiển thị
- Kiểm tra ứng dụng đang chạy trong khay hệ thống không
- Bấm icon khay hoặc nhấn `Ctrl + ``

### Không bắt được phím
- Đảm bảo ứng dụng đang chạy với quyền phù hợp
- Kiểm tra NumLock hoạt động

### Cài đặt không lưu
- Kiểm tra quyền ghi vào thư mục ứng dụng
- Kiểm tra tồn tại file `settings.json`

## Giấy phép

MIT License - xem file LICENSE để biết chi tiết

## Đóng góp

Hoan nghênh mọi đóng góp! Vui lòng tạo issue hoặc pull request.

---

**Xây dựng bằng**: C#, WPF, .NET 8.0
