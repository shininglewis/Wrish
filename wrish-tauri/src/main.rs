#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use std::fs;
use std::path::PathBuf;
use std::sync::Mutex;

use tauri::{
    menu::{Menu, MenuItem},
    tray::TrayIconBuilder,
    Manager, WindowEvent,
};

// ─── 应用状态 ───
struct AppState {
    current_file: Mutex<Option<PathBuf>>,
    is_dark: Mutex<bool>,
    is_dirty: Mutex<bool>,
}

// ═══════════════════════════════════════════════════════
//  Tauri Commands（前端调用）
// ═══════════════════════════════════════════════════════

#[tauri::command]
fn read_file(state: tauri::State<AppState>) -> Result<(String, String), String> {
    use rfd::FileDialog;

    let path = FileDialog::new()
        .add_filter("Text", &["txt", "md"])
        .add_filter("All", &["*"])
        .pick_file();

    if let Some(p) = path {
        let content = fs::read_to_string(&p).map_err(|e| e.to_string())?;
        let name = p.file_name().unwrap_or_default().to_string_lossy().to_string();
        *state.current_file.lock().unwrap() = Some(p);
        *state.is_dirty.lock().unwrap() = false;
        Ok((content, name))
    } else {
        Err("Cancelled".into())
    }
}

#[tauri::command]
fn save_file(content: String, state: tauri::State<AppState>) -> Result<String, String> {
    let mut file_lock = state.current_file.lock().unwrap();

    if let Some(ref path) = *file_lock {
        fs::write(path, content).map_err(|e| e.to_string())?;
        *state.is_dirty.lock().unwrap() = false;
        Ok(path.file_name().unwrap_or_default().to_string_lossy().to_string())
    } else {
        // 另存为
        drop(file_lock);
        save_as(content, state)
    }
}

#[tauri::command]
fn save_as(content: String, state: tauri::State<AppState>) -> Result<String, String> {
    use rfd::FileDialog;

    let path = FileDialog::new()
        .add_filter("Text", &["txt"])
        .add_filter("Markdown", &["md"])
        .add_filter("All", &["*"])
        .set_file_name("未命名.txt")
        .save_file();

    if let Some(p) = path {
        fs::write(&p, content).map_err(|e| e.to_string())?;
        let name = p.file_name().unwrap_or_default().to_string_lossy().to_string();
        *state.current_file.lock().unwrap() = Some(p);
        *state.is_dirty.lock().unwrap() = false;
        Ok(name)
    } else {
        Err("Cancelled".into())
    }
}

#[tauri::command]
fn auto_save(content: String, state: tauri::State<AppState>) {
    let file_lock = state.current_file.lock().unwrap();

    if let Some(ref path) = *file_lock {
        let _ = fs::write(path, content);
    } else {
        // 未命名文档写入临时文件
        let temp = std::env::current_dir().unwrap_or_default().join(".temp_draft.txt");
        let _ = fs::write(temp, content);
    }
}

#[tauri::command]
fn get_theme(state: tauri::State<AppState>) -> bool {
    *state.is_dark.lock().unwrap()
}

#[tauri::command]
fn set_theme(dark: bool, state: tauri::State<AppState>) {
    *state.is_dark.lock().unwrap() = dark;
}

#[tauri::command]
fn set_dirty(dirty: bool, state: tauri::State<AppState>) {
    *state.is_dirty.lock().unwrap() = dirty;
}

#[tauri::command]
fn is_dirty(state: tauri::State<AppState>) -> bool {
    *state.is_dirty.lock().unwrap()
}

// ═══════════════════════════════════════════════════════
//  主函数
// ═══════════════════════════════════════════════════════

fn main() {
    tauri::Builder::default()
        .manage(AppState {
            current_file: Mutex::new(None),
            is_dark: Mutex::new(true),
            is_dirty: Mutex::new(false),
        })
        .invoke_handler(tauri::generate_handler![
            read_file,
            save_file,
            save_as,
            auto_save,
            get_theme,
            set_theme,
            set_dirty,
            is_dirty,
        ])
        .setup(|app| {
            // ─── 系统托盘 ───
            let quit_i = MenuItem::with_id(app, "quit", "退出", true, None::<&str>)?;
            let show_i = MenuItem::with_id(app, "show", "显示", true, None::<&str>)?;
            let menu = Menu::with_items(app, &[&show_i, &quit_i])?;

            TrayIconBuilder::new()
                .icon(app.default_window_icon().unwrap().clone())
                .tooltip("Wrish")
                .menu(&menu)
                .on_menu_event(|app, event| match event.id.as_ref() {
                    "quit" => app.exit(0),
                    "show" => {
                        if let Some(window) = app.get_webview_window("main") {
                            let _ = window.show();
                            let _ = window.set_focus();
                        }
                    }
                    _ => {}
                })
                .on_tray_icon_event(|tray, event| {
                    if let tauri::tray::TrayIconEvent::DoubleClick { .. } = event {
                        if let Some(window) = tray.app_handle().get_webview_window("main") {
                            let _ = window.show();
                            let _ = window.set_focus();
                        }
                    }
                })
                .build(app)?;

            // ─── 窗口事件：关闭时隐藏到托盘 ───
            let window = app.get_webview_window("main").unwrap();
            let window_clone = window.clone();
            window.on_window_event(move |event| {
                if let WindowEvent::CloseRequested { api, .. } = event {
                    api.prevent_close();
                    let _ = window_clone.hide();
                }
            });

            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}