#![no_std]

mod imports;

use core::panic::PanicInfo;

#[panic_handler]
fn panic(_: &PanicInfo) -> ! {
    loop {}
}

#[no_mangle]
pub extern "C" fn on_init() -> i32 {
    unsafe {
        let msg = b"tier0";
        imports::log(0, msg.as_ptr() as i32, msg.len() as i32);
        imports::log_error(msg.as_ptr() as i32, msg.len() as i32);
        let _ = imports::get_editor_time();
        imports::show_progress(msg.as_ptr() as i32, 5, msg.as_ptr() as i32, 5, 0.5);
        imports::clear_progress();
    }
    0
}

#[no_mangle]
pub extern "C" fn on_shutdown() {}

#[no_mangle]
pub extern "C" fn on_menu_click() {}
