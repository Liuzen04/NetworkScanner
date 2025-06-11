# Network Scanner

## Giới thiệu
Network Scanner là ứng dụng Windows giúp quét và phân tích các thiết bị trong mạng LAN, hỗ trợ quét cổng, xuất báo cáo và mã hóa dữ liệu. Ứng dụng phù hợp cho sinh viên, kỹ thuật viên mạng, hoặc bất kỳ ai muốn kiểm tra, giám sát hệ thống mạng nội bộ.

## Tính năng chính
- Quét toàn bộ thiết bị trong mạng LAN (IP, MAC, Hostname, thời gian phản hồi)
- Quét cổng (port) cho từng thiết bị, hỗ trợ quét dải cổng hoặc các cổng phổ biến
- Mã hóa/giải mã dữ liệu bằng AES
- Xuất báo cáo kết quả quét
- Giao diện trực quan, dễ sử dụng, hỗ trợ tiếng Việt

## Hình ảnh minh họa
![Giao diện chính](images/screenshot_main.png)
![Quét cổng](images/screenshot_portscan.png)

## Hướng dẫn cài đặt
1. Yêu cầu: Windows 7/8/10/11, .NET Framework 4.7.2 trở lên
2. Clone hoặc tải source code từ GitHub:
   ```bash
   git clone https://github.com/Liuzen004/Network-Scanner.git
   ```
3. Mở solution `Detai5.sln` bằng Visual Studio 2017/2019/2022
4. Build và chạy ứng dụng (F5 hoặc Ctrl+F5)

## Hướng dẫn sử dụng
- **Quét thiết bị:**
  - Nhập dải IP cần quét (ví dụ: 192.168.1.1 đến 192.168.1.254)
  - Nhấn "Bắt đầu quét" để tìm các thiết bị đang online
- **Quét cổng:**
  - Chọn thiết bị, nhấn "Quét cổng"
  - Chọn dải cổng hoặc chỉ quét cổng phổ biến, nhấn "Bắt đầu quét"
- **Mã hóa dữ liệu:**
  - Vào menu "Công cụ" > "Mã hóa dữ liệu" để sử dụng chức năng mã hóa/giải mã
- **Xuất báo cáo:**
  - Nhấn "Xuất báo cáo" để lưu kết quả quét ra file

## Thông tin tác giả
- Họ tên: Phạm Trần Kim Long - Thanh Hải
- Email: Kimlong2004@gmail.com
- Github: https://github.com/Liuzen004
