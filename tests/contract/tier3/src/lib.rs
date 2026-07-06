#![no_std]
mod imports;
use core::panic::PanicInfo;
#[panic_handler]
fn panic(_: &PanicInfo) -> ! { loop {} }

#[no_mangle]
pub extern "C" fn on_init() -> i32 {
    unsafe {
        let mut out = [0u8; 16];
        let prop = b"m_Name";
        let _ = imports::get_object_path(0, out.as_mut_ptr() as i32, out.len() as i32);
        let _ = imports::get_serialized_property(0, prop.as_ptr() as i32, prop.len() as i32, out.as_mut_ptr() as i32, out.len() as i32);
        let _ = imports::get_component_count(0);
        let _ = imports::get_component_type_at(0, 0, out.as_mut_ptr() as i32, out.len() as i32);
    }
    0
}
#[no_mangle]
pub extern "C" fn on_shutdown() {}
#[no_mangle]
pub extern "C" fn on_menu_click() {}
