//! MVP example tool: logs current Unity Selection via editor host imports.

#![no_std]

use core::panic::PanicInfo;

#[panic_handler]
fn panic(_info: &PanicInfo) -> ! {
    loop {}
}

#[link(wasm_import_module = "editor_core")]
extern "C" {
    fn log(level: i32, ptr: i32, len: i32);
    fn get_editor_time() -> f64;
}

#[link(wasm_import_module = "editor_selection")]
extern "C" {
    fn get_active_object() -> i64;
    fn get_active_asset_path(out_ptr: i32, max_len: i32) -> i32;
    fn get_object_name(handle: i64, out_ptr: i32, max_len: i32) -> i32;
}

fn log_str(level: i32, text: &str) {
    unsafe {
        log(level, text.as_ptr() as i32, text.len() as i32);
    }
}

#[no_mangle]
pub extern "C" fn on_init() -> i32 {
    log_str(0, "selection-logger initialized");
    0
}

#[no_mangle]
pub extern "C" fn on_shutdown() {
    log_str(0, "selection-logger shutdown");
}

#[no_mangle]
pub extern "C" fn on_menu_click() {
    unsafe {
        let t = get_editor_time();
        log_str(0, "Selection Logger: running");

        let mut buf = [0u8; 64];
        let mut msg = [0u8; 256];
        let prefix = b"editor time = ";
        msg[..prefix.len()].copy_from_slice(prefix);
        let tlen = write_float(&mut msg[prefix.len()..], t);
        log_str(0, core::str::from_utf8_unchecked(&msg[..prefix.len() + tlen as usize]));

        let handle = get_active_object();
        if handle == 0 {
            log_str(0, "No active object selected.");
            return;
        }

        let name_len = get_object_name(handle, buf.as_mut_ptr() as i32, buf.len() as i32);
        if name_len > 0 {
            let name = core::str::from_utf8_unchecked(&buf[..name_len as usize]);
            let mut line = [0u8; 512];
            let head = b"Selected object: ";
            line[..head.len()].copy_from_slice(head);
            let n = name.as_bytes();
            let copy = n.len().min(line.len() - head.len());
            line[head.len()..head.len() + copy].copy_from_slice(&n[..copy]);
            log_str(0, core::str::from_utf8_unchecked(&line[..head.len() + copy]));
        }

        let mut path_buf = [0u8; 512];
        let path_len = get_active_asset_path(path_buf.as_mut_ptr() as i32, path_buf.len() as i32);
        if path_len > 0 {
            let path = core::str::from_utf8_unchecked(&path_buf[..path_len as usize]);
            let mut line = [0u8; 768];
            let head = b"Asset path: ";
            line[..head.len()].copy_from_slice(head);
            let p = path.as_bytes();
            let copy = p.len().min(line.len() - head.len());
            line[head.len()..head.len() + copy].copy_from_slice(&p[..copy]);
            log_str(0, core::str::from_utf8_unchecked(&line[..head.len() + copy]));
        } else {
            log_str(0, "Active object has no asset path.");
        }
    }
}

fn write_float(out: &mut [u8], value: f64) -> i32 {
    let int_part = value as i64;
    let frac = ((value - int_part as f64) * 1000.0) as i64;
    let mut tmp = [0u8; 32];
    let mut n = int_part;
    let mut i = 0;
    if n == 0 {
        tmp[0] = b'0';
        i = 1;
    } else {
        while n > 0 && i < tmp.len() {
            tmp[i] = b'0' + (n % 10) as u8;
            n /= 10;
            i += 1;
        }
        tmp[..i].reverse();
    }
    let mut len = i.min(out.len());
    out[..len].copy_from_slice(&tmp[..len]);
    if len + 4 < out.len() {
        out[len] = b'.';
        len += 1;
        let d0 = (frac / 100) as u8;
        let d1 = ((frac / 10) % 10) as u8;
        let d2 = (frac % 10) as u8;
        out[len] = b'0' + d0;
        out[len + 1] = b'0' + d1;
        out[len + 2] = b'0' + d2;
        len += 3;
    }
    len as i32
}
