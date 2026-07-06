//! Minimal hello-world WASM editor tool template.

#![no_std]

mod imports;

use core::panic::PanicInfo;

#[panic_handler]
fn panic(_info: &PanicInfo) -> ! {
    loop {}
}

fn log_str(level: i32, text: &str) {
    unsafe {
        imports::log(level, text.as_ptr() as i32, text.len() as i32);
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
        let t = imports::get_editor_time();
        log_str(0, "Hello Tool: running");
        let handle = imports::get_active_object();
        if handle == 0 {
            log_str(0, "No selection (that's OK for hello tool).");
        } else {
            log_str(0, "Something is selected.");
        }
        let _ = t;
    }
}
