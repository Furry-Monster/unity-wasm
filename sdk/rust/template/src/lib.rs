//! Minimal hello-world WASM editor tool template.

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
}

fn log_str(level: i32, text: &str) {
    unsafe {
        log(level, text.as_ptr() as i32, text.len() as i32);
    }
}

#[no_mangle]
pub extern "C" fn on_init() -> i32 {
    log_str(0, "hello-tool initialized");
    0
}

#[no_mangle]
pub extern "C" fn on_shutdown() {
    log_str(0, "hello-tool shutdown");
}

#[no_mangle]
pub extern "C" fn on_menu_click() {
    unsafe {
        let t = get_editor_time();
        log_str(0, "Hello Tool: running");
        let handle = get_active_object();
        if handle == 0 {
            log_str(0, "No selection (that's OK for hello tool).");
        } else {
            log_str(0, "Something is selected.");
        }
        let _ = t;
    }
}
