# Chạy PreEI R9 trên GitHub Actions (Connect mặc định, chọn được dataset khác)

## 1. Chuẩn bị

- Giải nén gói, tải **nội dung của thư mục `PreEI/`** lên thư mục gốc của repository GitHub. Đặc biệt phải giữ `.github/workflows/preei-run.yml` (đây là thư mục ẩn trên Windows).
- File `datasets/mushroom.txt` có sẵn để thử.
- **File `datasets/connect.txt` chưa có trong gói**. Bạn cần tải lên **chính file Connect đã gán profit của thí nghiệm**, không phải dữ liệu Connect thô. Mỗi dòng gồm các item ID nguyên, số nguyên cuối cùng là profit của giao dịch; thứ tự dòng phải giữ nguyên.
- Toàn bộ dataset được chia cùng một quy tắc: **80% ban đầu + 10 batch, mỗi batch 2%**; thứ tự giao dịch giữ nguyên và không tạo profit mới.

## 2. Chạy Connect

Vào **Actions → Run PreEI R9 → Run workflow** trên nhánh mặc định. Các trường được điền sẵn:

| Trường | Giá trị |
| --- | --- |
| `dataset` | `datasets/connect.txt` |
| `lower_threshold` | `2.5` |
| `upper_threshold` | `2.8` |
| `verify_reference` | `true` |

Nhấn **Run workflow** để chạy. GitHub tự động biên dịch bằng .NET 10, chia 11 batch và chạy thuật toán PreEI R9. Kiểm tra **11 số lượng erasable itemsets và quyết định re-scan** bằng `validation/connect_expected_itemset_counts.csv` được trích từ log gốc `validation/connect_2_5_2_8_original_log.txt`. Mốc quan trọng: stage 0 = 3.040.135; stage 5 = FullRescan / 1.239.090; stage 10 = 346.918.

**Quan trọng:** Nếu dữ liệu Connect không đúng bản gán profit/đúng thứ tự đã dùng để tạo log gốc, bước kiểm chứng có thể FAIL; điều đó không tự động chứng minh thuật toán sai. Kiểm tra dữ liệu và ngưỡng trước.

## 3. Chạy Mushroom hoặc dataset khác

- **Mushroom**: đặt `dataset` = `datasets/mushroom.txt`, `lower_threshold` = `1.05`, `upper_threshold` = `1.2`. GitHub sẽ kiểm chứng 11 stage bằng kết quả Mushroom gốc.
- **Dataset khác**: tải file .txt đã gán profit vào `datasets/`, nhập đường dẫn như `datasets/retail.txt`, sau đó nhập lower/upper thích hợp. Việc khai phá vẫn chạy 80% + 10 × 2%; kiểm chứng với log gốc được **bỏ qua** nếu chưa có bộ tham chiếu đúng cho dataset/ngưỡng ấy.
- Nếu muốn chạy Connect/Mushroom với profit hoặc ngưỡng khác mà không đối chiếu log, bỏ chọn `verify_reference`. Dòng `validation_status.txt` sẽ ghi `SKIP` chứ không ghi `PASS`.

## 4. Xem kết quả

Sau khi workflow xong, bấm vào lần chạy trong **Actions**, kéo tới **Artifacts**, tải `preei-r9-run-...`:

- `results/PreEI_<dataset>_results.csv`: 11 dòng stage và dòng TOTAL
- `results/PreEI_<dataset>_results.txt`: báo cáo dễ đọc
- `results/run_metadata.json`: ngưỡng, đường dẫn, SHA-256 dataset, phiên bản .NET, OS
- `results/validation_status.txt`: PASS, FAIL hoặc SKIP (nếu bước so sánh đã chạy)
- `batches/`: 11 file chia cùng `split_manifest.csv`

**Phạm vi đo:** CSV/TXT, giao diện, chia file ban đầu không tính vào runtime/MB mining. `PeakWorkingSet64` là đặc tính tiến trình và phụ thuộc môi trường chạy; không yêu cầu bằng máy tác giả. Phần thuật toán `PreEI/Mining/*.cs` và splitter `PreEI/DataSplitter.cs` được CLI liên kết trực tiếp, không chép/sửa logic.

## 5. Tự kiểm chứng CSV trong máy

```powershell
python validation/compare_reference_counts.py "PreEI_connect_results.csv" validation/connect_expected_itemset_counts.csv
python validation/compare_reference_counts.py "PreEI_mushroom_results.csv" validation/mushroom_expected_itemset_counts.csv
```

Chỉ so sánh với log nếu dùng **đúng file profit-augmented và đúng ngưỡng**. Không so runtime hoặc memory với file tham chiếu.

## 6. Trạng thái

Bộ mã đã được kiểm tra cú pháp/CSV/workflow ở mức tĩnh, **chưa biên dịch hoặc chạy thực tế bằng GitHub Actions trong môi trường này**. Sau khi tải lên repository, cần chạy thử ít nhất một job thật và xem kết quả kiểm chứng.
