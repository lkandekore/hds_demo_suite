use reqwest::Client;
use crate::models::{FaultSignature, RegisterRequest};

#[derive(Clone)]
pub struct HdsClient {
    http: Client,
    pub base_url: String,
}

impl HdsClient {
    pub fn new() -> Self {
        Self {
            http: Client::new(),
            base_url: "http://localhost:5005".into(),
        }
    }

    pub async fn register_app(&self, name: &str, version: &str)
                              -> Result<String, reqwest::Error>
    {
        let body = RegisterRequest {
            application: name.into(),
            version: version.into(),
        };

        let res = self.http
            .post(format!("{}/api/v1/apps/register", self.base_url))
            .json(&body)
            .send()
            .await?;

        Ok(res.text().await?)
    }

    pub async fn report_fault(&self, fault: &FaultSignature)
                              -> Result<String, reqwest::Error>
    {
        let res = self.http
            .post(format!("{}/api/v1/faults/report", self.base_url))
            .json(fault)
            .send()
            .await?;

        Ok(res.text().await?)
    }
}
