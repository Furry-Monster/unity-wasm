//! Lists component types on the selected GameObject / Prefab root.

#![no_std]

mod imports;

use core::panic::PanicInfo;

#[panic_handler]
fn panic(_info: &PanicInfo) -> ! {
    loop {}
}

fn log_bytes(level: i32, bytes: &[u8]) {
    unsafe {
        imports::log(level, bytes.as_ptr() as i32, bytes.len() as i32);
    }
}

fn write_path(out: &mut [u8], handle: i64) -> usize {
    unsafe {
        let len = imports::get_object_path(handle, out.as_mut_ptr() as i32, out.len() as i32);
        if len > 0 {
            len as usize
        } else {
            0
        }
    }
}

#[no_mangle]
pub extern "C" fn on_init() -> i32 {
    log_bytes(0, b"prefab-inspector-lite initialized");
    0
}

#[no_mangle]
pub extern "C" fn on_shutdown() {}

#[no_mangle]
pub extern "C" fn on_menu_click() {
    unsafe {
        let handle = imports::get_active_object();
        if handle == 0 {
            log_bytes(0, b"No selection.");
            return;
        }

        let mut path_buf = [0u8; 512];
        let path_len = write_path(&mut path_buf, handle);
        if path_len > 0 {
            let mut line = [0u8; 768];
            let head = b"Object path: ";
            line[..head.len()].copy_from_slice(head);
            let copy = path_len.min(line.len() - head.len());
            line[head.len()..head.len() + copy].copy_from_slice(&path_buf[..copy]);
            log_bytes(0, &line[..head.len() + copy]);
        }

        let count = imports::get_component_count(handle);
        if count <= 0 {
            log_bytes(0, b"No components found.");
            return;
        }

        let mut summary = [0u8; 64];
        let head = b"Components: ";
        summary[..head.len()].copy_from_slice(head);
        let n = count as i64;
        let mut tmp = [0u8; 12];
        let mut digits = 0usize;
        let mut v = n;
        if v == 0 {
            tmp[0] = b'0';
            digits = 1;
        } else {
            while v > 0 && digits < tmp.len() {
                tmp[digits] = b'0' + (v % 10) as u8;
                v /= 10;
                digits += 1;
            }
            tmp[..digits].reverse();
        }
        let pos = head.len();
        summary[pos..pos + digits].copy_from_slice(&tmp[..digits]);
        log_bytes(0, &summary[..pos + digits]);

        let mut type_buf = [0u8; 128];
        for index in 0..count {
            let type_len = imports::get_component_type_at(
                handle,
                index,
                type_buf.as_mut_ptr() as i32,
                type_buf.len() as i32,
            );
            if type_len <= 0 {
                continue;
            }
            let mut line = [0u8; 192];
            let prefix = b"  - ";
            line[..prefix.len()].copy_from_slice(prefix);
            let copy = (type_len as usize).min(line.len() - prefix.len());
            line[prefix.len()..prefix.len() + copy].copy_from_slice(&type_buf[..copy]);
            log_bytes(0, &line[..prefix.len() + copy]);
        }
    }
}
