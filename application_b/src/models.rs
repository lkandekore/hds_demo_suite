use serde::{Serialize, Deserialize};
use chrono::{DateTime, Utc};

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct CaptureRequest {
    #[serde(rename = "LogFileLocation")]
    pub log_file_location: String,

    #[serde(rename = "Capture")]
    pub capture: Vec<String>,

    #[serde(rename = "Environment")]
    pub environment: Vec<String>,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct FaultSignature {
    #[serde(rename = "ApplicationName")]
    pub application_name: String,

    #[serde(rename = "FaultCode")]
    pub fault_code: String,

    #[serde(rename = "Type")]
    pub type_field: String,

    #[serde(rename = "Severity")]
    pub severity: String,

    #[serde(rename = "Description")]
    pub description: String,

    #[serde(rename = "Timestamp")]
    pub timestamp: DateTime<Utc>,

    #[serde(rename = "CaptureRequest")]
    pub capture_request: CaptureRequest,
}

#[derive(Serialize, Deserialize, Debug)]
pub struct RegisterRequest {
    pub application: String,
    pub version: String,
}
