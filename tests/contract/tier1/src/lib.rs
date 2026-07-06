#![no_std]
mod imports;
use core::panic::PanicInfo;
#[panic_handler]
fn panic(_: &PanicInfo) -> ! { loop {} }

#[no_mangle]
pub extern "C" fn on_init() -> i32 {
    unsafe {
        let _ = imports::get_active_object();
        let _ = imports::get_active_objects_count();
        let _ = imports::get_active_object_at(0);
        let mut buf = [0u8; 8];
        let _ = imports::get_active_asset_path(buf.as_mut_ptr() as i32, buf.len() as i32);
        let _ = imports::get_object_name(0, buf.as_mut_ptr() as i32, buf.len() as i32);
    }
    0
}
#[no_mangle]
pub extern "C" fn on_shutdown() {}
#[no_mangle]
pub extern "C" fn on_menu_click() {}
