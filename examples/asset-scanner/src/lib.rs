//! Scans Texture assets under Assets/ and reports count + sample paths.

#![no_std]

use core::panic::PanicInfo;

#[panic_handler]
fn panic(_info: &PanicInfo) -> ! {
    loop {}
}

#[link(wasm_import_module = "editor_core")]
extern "C" {
    fn log(level: i32, ptr: i32, len: i32);
    fn show_progress(title_ptr: i32, title_len: i32, info_ptr: i32, info_len: i32, progress: f32);
    fn clear_progress();
}

#[link(wasm_import_module = "editor_assets")]
extern "C" {
    fn find_assets_count(filter_ptr: i32, filter_len: i32, paths_ptr: i32, paths_len: i32) -> i32;
    fn find_asset_at(
        filter_ptr: i32,
        filter_len: i32,
        paths_ptr: i32,
        paths_len: i32,
        index: i32,
        out_ptr: i32,
        max_len: i32,
    ) -> i32;
}

const FILTER: &[u8] = b"t:Texture";
const SEARCH_PATHS: &[u8] = b"Assets\0";
const PROGRESS_EVERY: i32 = 50;
const SAMPLE_COUNT: i32 = 10;

fn log_str(level: i32, text: &str) {
    unsafe {
        log(level, text.as_ptr() as i32, text.len() as i32);
    }
}

fn log_bytes(level: i32, bytes: &[u8]) {
    unsafe {
        log(level, bytes.as_ptr() as i32, bytes.len() as i32);
    }
}

#[no_mangle]
pub extern "C" fn on_init() -> i32 {
    log_str(0, "asset-scanner initialized");
    0
}

#[no_mangle]
pub extern "C" fn on_shutdown() {
    log_str(0, "asset-scanner shutdown");
}

#[no_mangle]
pub extern "C" fn on_menu_click() {
    unsafe {
        let count = find_assets_count(
            FILTER.as_ptr() as i32,
            FILTER.len() as i32,
            SEARCH_PATHS.as_ptr() as i32,
            SEARCH_PATHS.len() as i32,
        );

        if count <= 0 {
            log_str(0, "No Texture assets found under Assets/");
            clear_progress();
            return;
        }

        let title = b"Asset Scanner";
        let mut path_buf = [0u8; 512];
        let mut sample_logged = 0i32;

        for index in 0..count {
            if index % PROGRESS_EVERY == 0 || index == count - 1 {
                let (info, info_len) = format_progress(index + 1, count);
                show_progress(
                    title.as_ptr() as i32,
                    title.len() as i32,
                    info.as_ptr() as i32,
                    info_len as i32,
                    (index + 1) as f32 / count as f32,
                );
            }

            let path_len = find_asset_at(
                FILTER.as_ptr() as i32,
                FILTER.len() as i32,
                SEARCH_PATHS.as_ptr() as i32,
                SEARCH_PATHS.len() as i32,
                index,
                path_buf.as_mut_ptr() as i32,
                path_buf.len() as i32,
            );

            if path_len > 0 && sample_logged < SAMPLE_COUNT {
                let path = core::str::from_utf8_unchecked(&path_buf[..path_len as usize]);
                let mut line = [0u8; 768];
                let head = b"  ";
                line[..head.len()].copy_from_slice(head);
                let bytes = path.as_bytes();
                let copy = bytes.len().min(line.len() - head.len());
                line[head.len()..head.len() + copy].copy_from_slice(&bytes[..copy]);
                log_bytes(0, &line[..head.len() + copy]);
                sample_logged += 1;
            }
        }

        let (summary, summary_len) = format_summary(count, sample_logged);
        log_bytes(0, &summary[..summary_len]);
        clear_progress();
    }
}

fn format_progress(current: i32, total: i32) -> ([u8; 64], usize) {
    let mut out = [0u8; 64];
    let prefix = b"Scanning ";
    out[..prefix.len()].copy_from_slice(prefix);
    let mut pos = prefix.len();
    pos += write_int(&mut out[pos..], current as i64);
    if pos + 4 <= out.len() {
        out[pos..pos + 4].copy_from_slice(b" / ");
        pos += 4;
        pos += write_int(&mut out[pos..], total as i64);
    }
    (out, pos)
}

fn format_summary(count: i32, sampled: i32) -> ([u8; 128], usize) {
    let mut out = [0u8; 128];
    let head = b"Found ";
    out[..head.len()].copy_from_slice(head);
    let mut pos = head.len();
    pos += write_int(&mut out[pos..], count as i64);
    let tail = b" Texture asset(s); sample paths above (max ";
    let tail_len = tail.len().min(out.len() - pos);
    out[pos..pos + tail_len].copy_from_slice(&tail[..tail_len]);
    pos += tail_len;
    pos += write_int(&mut out[pos..], sampled as i64);
    if pos < out.len() {
        out[pos] = b')';
        pos += 1;
    }
    (out, pos)
}

fn write_int(out: &mut [u8], mut n: i64) -> usize {
    if out.is_empty() {
        return 0;
    }
    if n == 0 {
        out[0] = b'0';
        return 1;
    }
    let mut tmp = [0u8; 20];
    let mut i = 0;
    while n > 0 && i < tmp.len() {
        tmp[i] = b'0' + (n % 10) as u8;
        n /= 10;
        i += 1;
    }
    tmp[..i].reverse();
    let len = i.min(out.len());
    out[..len].copy_from_slice(&tmp[..len]);
    len
}
