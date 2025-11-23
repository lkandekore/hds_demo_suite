mod models;
mod hds_client;

use eframe::egui;
use chrono::Utc;
use crate::hds_client::HdsClient;
use crate::models::{FaultSignature, CaptureRequest};
use tokio::runtime::Runtime;
use std::sync::mpsc::{self, Sender, Receiver};

#[derive(Clone)]
struct LogItem {
    text: String,
    color: egui::Color32,
}

struct App {
    client: HdsClient,
    rt: Runtime,
    log_tx: Sender<LogItem>,
    log_rx: Receiver<LogItem>,
    log: Vec<LogItem>,
}

impl Default for App {
    fn default() -> Self {
        let (log_tx, log_rx) = mpsc::channel();
        let rt = Runtime::new().unwrap();
        let client = HdsClient::new();

        // AUTO REGISTER
        {
            let tx = log_tx.clone();
            let client_clone = client.clone();

            rt.spawn(async move {
                match client_clone.register_app("Application B", "1.0.0").await {
                    Ok(resp) => tx.send(LogItem {
                        text: format!("REGISTER OK:\n{}", resp),
                        color: egui::Color32::LIGHT_GREEN,
                    }).unwrap(),

                    Err(e) => tx.send(LogItem {
                        text: format!("REGISTER FAILED: {}", e),
                        color: egui::Color32::RED,
                    }).unwrap(),
                }
            });
        }

        Self {
            client,
            rt,
            log_tx,
            log_rx,
            log: vec![LogItem {
                text: "Application B starting…".into(),
                color: egui::Color32::GRAY,
            }],
        }
    }
}

impl eframe::App for App {

    fn update(&mut self, ctx: &egui::Context, _frame: &mut eframe::Frame) {

        // Pull async logs
        while let Ok(item) = self.log_rx.try_recv() {
            self.log.push(item);
        }

        // 🌙 Modern DARK THEME
        ctx.set_visuals(egui::Visuals {
            dark_mode: true,

            // Background dark grey
            window_fill: egui::Color32::from_rgb(28, 29, 33),
            panel_fill:  egui::Color32::from_rgb(36, 37, 41),

            override_text_color: Some(egui::Color32::from_rgb(230, 230, 230)),

            // Softer rounding
            window_rounding: 12.0.into(),

            window_shadow: egui::epaint::Shadow {
                offset: [0.0, 2.0].into(),
                blur: 22.0,
                spread: 0.0,
                color: egui::Color32::from_rgba_unmultiplied(0, 0, 0, 150),
            },

            ..Default::default()
        });

        egui::CentralPanel::default().show(ctx, |ui| {

            ui.add_space(6.0);

            // BUTTON BAR — clean layout
            ui.horizontal_wrapped(|ui| {
                ui.spacing_mut().item_spacing = egui::vec2(12.0, 12.0);

                if mac_button(ui, "Null Pointer Fault").clicked() {
                    exception(ui, self, "simulated null pointer");
                    self.send_fault(build_fault_np());
                }

                if mac_button(ui, "Out of Range Fault").clicked() {
                    exception(ui, self, "simulated index out of range");
                    self.send_fault(build_fault_out_of_range());
                }

                if mac_button(ui, "Config Mismatch").clicked() {
                    exception(ui, self, "config missing");
                    self.send_fault(build_fault_config());
                }

                if mac_button(ui, "Watchdog Timeout").clicked() {
                    exception(ui, self, "simulated watchdog timeout");
                    self.send_fault(build_fault_watchdog());
                }
            });

            ui.separator();
            ui.heading("Event Log");

            // LOG SCROLL WINDOW
            egui::ScrollArea::vertical()
                .auto_shrink([false; 2])
                .show(ui, |ui| {
                    for entry in &self.log {
                        ui.colored_label(entry.color, &entry.text);
                        ui.add_space(4.0);
                    }
                });
        });

        ctx.request_repaint();
    }
}

// helper for adding red exceptions
fn exception(_ui: &egui::Ui, app: &mut App, message: &str) {
    app.log.push(LogItem {
        text: format!("Caught exception: {}", message),
        color: egui::Color32::RED,
    });
}

// Beautiful dark-mode buttons
fn mac_button(ui: &mut egui::Ui, label: &str) -> egui::Response {
    ui.add(
        egui::Button::new(label)
            .rounding(10.0)
            .fill(egui::Color32::from_rgb(55, 56, 60))
            .stroke(egui::Stroke::new(1.5, egui::Color32::from_rgb(90, 90, 95)))
            .min_size(egui::vec2(180.0, 48.0))
    )
}

impl App {
    fn send_fault(&mut self, fault: FaultSignature) {
        let json = serde_json::to_string_pretty(&fault).unwrap();

        // grey outgoing JSON
        self.log.push(LogItem {
            text: format!("Outgoing Fault:\n{}", json),
            color: egui::Color32::GRAY,
        });

        let tx = self.log_tx.clone();
        let client = self.client.clone();

        self.rt.spawn(async move {
            match client.report_fault(&fault).await {
                Ok(resp) => tx.send(LogItem {
                    text: format!("FAULT OK:\n{}", resp),
                    color: egui::Color32::LIGHT_GREEN,
                }).unwrap(),

                Err(e) => tx.send(LogItem {
                    text: format!("FAULT FAILED: {}", e),
                    color: egui::Color32::RED,
                }).unwrap(),
            }
        });
    }
}

// -------- Fault Builders --------

fn build_fault_np() -> FaultSignature {
    FaultSignature {
        application_name: "Application B".into(),
        fault_code: "F018".into(),
        type_field: "F0".into(),
        severity: "Error".into(),
        description: "Null pointer in Rust".into(),
        timestamp: Utc::now(),

        capture_request: CaptureRequest {
            log_file_location: "/var/log/app_rust.log".into(),
            capture: vec!["DLTLogs".into(), "MemoryDump".into()],
            environment: vec!["CPU".into(), "RAM".into()],
        },
    }
}

fn build_fault_out_of_range() -> FaultSignature {
    FaultSignature {
        application_name: "Application B".into(),
        fault_code: "F021".into(),
        type_field: "F9".into(),
        severity: "Critical".into(),
        description: "Array out of range".into(),
        timestamp: Utc::now(),

        capture_request: CaptureRequest {
            log_file_location: "/var/logs/app/application_b.dlt".into(),
            capture: vec!["DLTLogs".into(), "PCAP".into()],
            environment: vec!["CPU".into(), "RAM".into()],
        },
    }
}

fn build_fault_config() -> FaultSignature {
    FaultSignature {
        application_name: "Application B".into(),
        fault_code: "F01C".into(),
        type_field: "F4".into(),
        severity: "Warning".into(),
        description: "Configuration mismatch".into(),
        timestamp: Utc::now(),

        capture_request: CaptureRequest {
            log_file_location: "/var/logs/app/application_b.dlt".into(),
            capture: vec!["DLTLogs".into()],
            environment: vec!["DISK".into(), "RAM".into()],
        },
    }
}

fn build_fault_watchdog() -> FaultSignature {
    FaultSignature {
        application_name: "Application B".into(),
        fault_code: "F01A".into(),
        type_field: "F2".into(),
        severity: "Error".into(),
        description: "Watchdog timeout".into(),
        timestamp: Utc::now(),

        capture_request: CaptureRequest {
            log_file_location: "/var/logs/app/application_b.dlt".into(),
            capture: vec!["DLTLogs".into(), "PCAP".into()],
            environment: vec!["CPU".into(), "RAM".into(), "THREADS".into()],
        },
    }
}

// ------- main ---------

fn main() -> eframe::Result<()> {
    env_logger::init();

    let native_options = eframe::NativeOptions::default();
    eframe::run_native(
        "Application B – Dark Mode",
        native_options,
        Box::new(|_cc| Box::<App>::default()),
    )
}
