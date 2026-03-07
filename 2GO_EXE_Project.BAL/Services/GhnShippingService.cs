using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using _2GO_EXE_Project.BAL.DTOs.Shipping;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.BAL.Settings;

namespace _2GO_EXE_Project.BAL.Services;

public class GhnShippingService : IGhnShippingService
{
    private readonly HttpClient _httpClient;
    private readonly GhnSettings _settings;
    private readonly ILogger<GhnShippingService> _logger;

    public GhnShippingService(HttpClient httpClient, IOptions<GhnSettings> options, ILogger<GhnShippingService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value ?? new GhnSettings();
        _logger = logger;
    }

    public async Task<string> CreateOrderAsync(CreateGhnShippingRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Token) || _settings.ShopId <= 0)
        {
            throw new InvalidOperationException("Chưa cấu hình GHN.");
        }

        var payload = new Dictionary<string, object?>
        {
            ["payment_type_id"] = request.PaymentTypeId,
            ["required_note"] = request.RequiredNote,
            ["to_name"] = request.ToName,
            ["to_phone"] = request.ToPhone,
            ["to_address"] = request.ToAddress,
            ["to_ward_code"] = request.ToWardCode,
            ["to_district_id"] = request.ToDistrictId,
            ["weight"] = request.Weight,
            ["length"] = request.Length,
            ["width"] = request.Width,
            ["height"] = request.Height,
            ["service_type_id"] = request.ServiceTypeId,
            ["note"] = request.Note,
            ["items"] = request.Items.Select(i => new Dictionary<string, object?>
            {
                ["name"] = i.Name,
                ["code"] = i.Code,
                ["quantity"] = i.Quantity,
                ["price"] = i.Price,
                ["weight"] = i.Weight,
                ["length"] = i.Length,
                ["width"] = i.Width,
                ["height"] = i.Height
            })
        };

        if (!string.IsNullOrWhiteSpace(request.FromName)) payload["from_name"] = request.FromName;
        if (!string.IsNullOrWhiteSpace(request.FromPhone)) payload["from_phone"] = request.FromPhone;
        if (!string.IsNullOrWhiteSpace(request.FromAddress)) payload["from_address"] = request.FromAddress;
        if (!string.IsNullOrWhiteSpace(request.FromWardCode)) payload["from_ward_code"] = request.FromWardCode;
        if (request.FromDistrictId.HasValue) payload["from_district_id"] = request.FromDistrictId.Value;

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/v2/shipping-order/create";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        httpRequest.Headers.Add("Token", _settings.Token);
        httpRequest.Headers.Add("ShopId", _settings.ShopId.ToString());

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("GHN create order failed: {Status} {Body}", response.StatusCode, raw);
            throw new InvalidOperationException("Tạo đơn GHN thất bại.");
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var data) &&
                data.TryGetProperty("order_code", out var codeProp) &&
                codeProp.ValueKind == JsonValueKind.String)
            {
                return codeProp.GetString() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
            // ignore parse error, handle below
        }

        throw new InvalidOperationException("Phản hồi GHN thiếu order_code.");
    }

    public async Task<IReadOnlyList<GhnProvinceResponse>> GetProvincesAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{_settings.BaseUrl.TrimEnd('/')}/master-data/province";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Token", _settings.Token);
        req.Headers.Add("ShopId", _settings.ShopId.ToString());

        using var resp = await _httpClient.SendAsync(req, cancellationToken);
        var raw = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("GHN provinces failed: {Status} {Body}", resp.StatusCode, raw);
            throw new InvalidOperationException("Lấy danh sách tỉnh GHN thất bại.");
        }

        using var doc = JsonDocument.Parse(raw);
        var data = doc.RootElement.TryGetProperty("data", out var dataProp) ? dataProp : default;
        if (data.ValueKind != JsonValueKind.Array) return Array.Empty<GhnProvinceResponse>();

        var results = new List<GhnProvinceResponse>();
        foreach (var item in data.EnumerateArray())
        {
            if (item.TryGetProperty("ProvinceID", out var idProp) &&
                item.TryGetProperty("ProvinceName", out var nameProp))
            {
                if (idProp.TryGetInt32(out var id))
                {
                    results.Add(new GhnProvinceResponse(id, nameProp.GetString() ?? string.Empty));
                }
            }
        }
        return results;
    }

    public async Task<IReadOnlyList<GhnDistrictResponse>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default)
    {
        var url = $"{_settings.BaseUrl.TrimEnd('/')}/master-data/district?province_id={provinceId}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Token", _settings.Token);
        req.Headers.Add("ShopId", _settings.ShopId.ToString());

        using var resp = await _httpClient.SendAsync(req, cancellationToken);
        var raw = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("GHN districts failed: {Status} {Body}", resp.StatusCode, raw);
            throw new InvalidOperationException("Lấy danh sách quận/huyện GHN thất bại.");
        }

        using var doc = JsonDocument.Parse(raw);
        var data = doc.RootElement.TryGetProperty("data", out var dataProp) ? dataProp : default;
        if (data.ValueKind != JsonValueKind.Array) return Array.Empty<GhnDistrictResponse>();

        var results = new List<GhnDistrictResponse>();
        foreach (var item in data.EnumerateArray())
        {
            if (item.TryGetProperty("DistrictID", out var idProp) &&
                item.TryGetProperty("DistrictName", out var nameProp) &&
                item.TryGetProperty("ProvinceID", out var provinceProp))
            {
                if (idProp.TryGetInt32(out var id) && provinceProp.TryGetInt32(out var prov))
                {
                    results.Add(new GhnDistrictResponse(id, nameProp.GetString() ?? string.Empty, prov));
                }
            }
        }
        return results;
    }

    public async Task<IReadOnlyList<GhnWardResponse>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default)
    {
        var url = $"{_settings.BaseUrl.TrimEnd('/')}/master-data/ward?district_id={districtId}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Token", _settings.Token);
        req.Headers.Add("ShopId", _settings.ShopId.ToString());

        using var resp = await _httpClient.SendAsync(req, cancellationToken);
        var raw = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("GHN wards failed: {Status} {Body}", resp.StatusCode, raw);
            throw new InvalidOperationException("Lấy danh sách phường/xã GHN thất bại.");
        }

        using var doc = JsonDocument.Parse(raw);
        var data = doc.RootElement.TryGetProperty("data", out var dataProp) ? dataProp : default;
        if (data.ValueKind != JsonValueKind.Array) return Array.Empty<GhnWardResponse>();

        var results = new List<GhnWardResponse>();
        foreach (var item in data.EnumerateArray())
        {
            if (item.TryGetProperty("WardCode", out var codeProp) &&
                item.TryGetProperty("WardName", out var nameProp) &&
                item.TryGetProperty("DistrictID", out var districtProp))
            {
                if (districtProp.TryGetInt32(out var dist))
                {
                    results.Add(new GhnWardResponse(codeProp.GetString() ?? string.Empty, nameProp.GetString() ?? string.Empty, dist));
                }
            }
        }
        return results;
    }

    public async Task<GhnFeeResponse> GetFeeAsync(GhnFeeRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Token) || _settings.ShopId <= 0)
        {
            throw new InvalidOperationException("Chưa cấu hình GHN.");
        }

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/v2/shipping-order/fee";
        var payload = new Dictionary<string, object?>
        {
            ["from_district_id"] = request.FromDistrictId,
            ["from_ward_code"] = request.FromWardCode,
            ["to_district_id"] = request.ToDistrictId,
            ["to_ward_code"] = request.ToWardCode,
            ["weight"] = request.Weight,
            ["length"] = request.Length,
            ["width"] = request.Width,
            ["height"] = request.Height,
            ["service_type_id"] = request.ServiceTypeId
        };

        if (request.InsuranceValue.HasValue) payload["insurance_value"] = request.InsuranceValue.Value;
        if (request.ServiceId.HasValue) payload["service_id"] = request.ServiceId.Value;

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        req.Headers.Add("Token", _settings.Token);
        req.Headers.Add("ShopId", _settings.ShopId.ToString());

        using var resp = await _httpClient.SendAsync(req, cancellationToken);
        var raw = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("GHN fee failed: {Status} {Body}", resp.StatusCode, raw);
            throw new InvalidOperationException("Tính phí GHN thất bại.");
        }

        using var doc = JsonDocument.Parse(raw);
        var data = doc.RootElement.TryGetProperty("data", out var dataProp) ? dataProp : default;
        if (data.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Phản hồi phí GHN thiếu dữ liệu.");
        }

        long GetLong(string name)
        {
            return data.TryGetProperty(name, out var p) && p.TryGetInt64(out var v) ? v : 0;
        }

        return new GhnFeeResponse(
            GetLong("total"),
            GetLong("service_fee"),
            GetLong("insurance_fee"),
            GetLong("pick_station_fee"),
            GetLong("coupon_value"),
            GetLong("r2s_fee"));
    }

    public async Task<GhnCancelResponse> CancelAsync(GhnCancelRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Token) || _settings.ShopId <= 0)
        {
            throw new InvalidOperationException("Chưa cấu hình GHN.");
        }

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/v2/switch-status/cancel";
        var payload = new Dictionary<string, object?>
        {
            ["order_codes"] = request.OrderCodes
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        req.Headers.Add("Token", _settings.Token);
        req.Headers.Add("ShopId", _settings.ShopId.ToString());

        using var resp = await _httpClient.SendAsync(req, cancellationToken);
        var raw = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("GHN cancel failed: {Status} {Body}", resp.StatusCode, raw);
            throw new InvalidOperationException("Hủy đơn GHN thất bại.");
        }

        var results = new List<GhnCancelResult>();
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                if (data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        results.Add(ParseCancelResult(item));
                    }
                }
                else if (data.ValueKind == JsonValueKind.Object)
                {
                    results.Add(ParseCancelResult(data));
                }
            }
        }
        catch (JsonException)
        {
            // ignore parse errors
        }

        return new GhnCancelResponse(results);
    }

    public async Task<GhnPrintTokenResponse> GetPrintTokenAsync(GhnPrintTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Token) || _settings.ShopId <= 0)
        {
            throw new InvalidOperationException("Chưa cấu hình GHN.");
        }

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/v2/a5/gen-token";
        var payload = new Dictionary<string, object?>
        {
            ["order_codes"] = request.OrderCodes
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        req.Headers.Add("Token", _settings.Token);
        req.Headers.Add("ShopId", _settings.ShopId.ToString());

        using var resp = await _httpClient.SendAsync(req, cancellationToken);
        var raw = await resp.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInformation("GHN print token response: {Body}", raw);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("GHN print token failed: {Status} {Body}", resp.StatusCode, raw);
            throw new InvalidOperationException("Lấy token in GHN thất bại.");
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                var token = data.TryGetProperty("token", out var tokenProp) ? tokenProp.GetString() ?? string.Empty : string.Empty;
                var a5 = data.TryGetProperty("print_a5_url", out var a5Prop) ? a5Prop.GetString() ?? string.Empty : string.Empty;
                var p80 = data.TryGetProperty("print_80x80_url", out var p80Prop) ? p80Prop.GetString() ?? string.Empty : string.Empty;
                var p52 = data.TryGetProperty("print_52x70_url", out var p52Prop) ? p52Prop.GetString() ?? string.Empty : string.Empty;
                return new GhnPrintTokenResponse(token, a5, p80, p52);
            }
        }
        catch (JsonException)
        {
            // ignore parse errors
        }

        return new GhnPrintTokenResponse(string.Empty, string.Empty, string.Empty, string.Empty);
    }

    public async Task<GhnOrderInfoResponse> GetOrderInfoAsync(string orderCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Token) || _settings.ShopId <= 0)
        {
            throw new InvalidOperationException("Chưa cấu hình GHN.");
        }

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/v2/shipping-order/detail";
        var payload = new Dictionary<string, object?>
        {
            ["order_code"] = orderCode
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        req.Headers.Add("Token", _settings.Token);
        req.Headers.Add("ShopId", _settings.ShopId.ToString());

        using var resp = await _httpClient.SendAsync(req, cancellationToken);
        var raw = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("GHN order detail failed: {Status} {Body}", resp.StatusCode, raw);
            throw new InvalidOperationException("Lấy chi tiết đơn GHN thất bại.");
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                string? GetString(string name) =>
                    data.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
                        ? prop.GetString()
                        : null;

                var code = GetString("order_code") ?? orderCode;
                var status = GetString("status");
                var statusName = GetString("status_name");
                var updatedDate = GetString("updated_date");

                return new GhnOrderInfoResponse(code, status, statusName, updatedDate, raw);
            }
        }
        catch (JsonException)
        {
            // ignore parse errors and fall through
        }

        return new GhnOrderInfoResponse(orderCode, null, null, null, raw);
    }

    private static GhnCancelResult ParseCancelResult(JsonElement item)
    {
        var orderCode = item.TryGetProperty("order_code", out var codeProp)
            ? codeProp.GetString() ?? string.Empty
            : string.Empty;
        var result = false;
        if (item.TryGetProperty("result", out var resultProp))
        {
            if (resultProp.ValueKind == JsonValueKind.True || resultProp.ValueKind == JsonValueKind.False)
            {
                result = resultProp.GetBoolean();
            }
            else if (resultProp.ValueKind == JsonValueKind.Number && resultProp.TryGetInt32(out var num))
            {
                result = num != 0;
            }
        }
        var message = item.TryGetProperty("message", out var messageProp)
            ? messageProp.GetString()
            : null;
        return new GhnCancelResult(orderCode, result, message);
    }
}

