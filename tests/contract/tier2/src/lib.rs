#![no_std]
mod imports;
use core::panic::PanicInfo;
#[panic_handler]
fn panic(_: &PanicInfo) -> ! { loop {} }

#[no_mangle]
pub extern "C" fn on_init() -> i32 {
    unsafe {
        let path = b"Assets\0";
        let filter = b"t:Texture";
        let _ = imports::asset_exists(path.as_ptr() as i32, 6);
        let _ = imports::find_assets_count(filter.as_ptr() as i32, 9, path.as_ptr() as i32, 7);
        let mut out = [0u8; 16];
        let _ = imports::find_asset_at(filter.as_ptr() as i32, 9, path.as_ptr() as i32, 7, 0, out.as_mut_ptr() as i32, out.len() as i32);
        let _ = imports::load_text_asset(path.as_ptr() as i32, 6, out.as_mut_ptr() as i32, out.len() as i32);
        let _ = imports::write_bulk_payload(0, path.as_ptr() as i32, 0, 0);
    }
    0
}
#[no_mangle]
pub extern "C" fn on_shutdown() {}
#[no_mangle]
pub extern "C" fn on_menu_click() {}
