import { useState } from 'react';
import './AuthPage.css';

export default function AuthPage({ initialMode = 'login', onSuccess, onGoHome }) {
  const [mode, setMode] = useState(initialMode); // 'login' hoặc 'register'

  // Form states cho Đăng nhập
  const [loginInput, setLoginInput] = useState('');
  const [loginPassword, setLoginPassword] = useState('');

  // Form states cho Đăng ký
  const [regUsername, setRegUsername] = useState('');
  const [regFullName, setRegFullName] = useState('');
  const [regEmail, setRegEmail] = useState('');
  const [regPassword, setRegPassword] = useState('');
  const [regConfirmPassword, setRegConfirmPassword] = useState('');

  // UI states
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  // Chuyển đổi giữa Đăng nhập và Đăng ký
  const switchMode = (newMode) => {
    setMode(newMode);
    setErrorMessage('');
    setSuccessMessage('');
  };

  // 1. Xử lý Đăng nhập
  const handleLoginSubmit = async (e) => {
    e.preventDefault();
    setErrorMessage('');
    setSuccessMessage('');

    if (!loginInput.trim()) {
      setErrorMessage('Vui lòng nhập Email hoặc Tên đăng nhập.');
      return;
    }
    if (!loginPassword) {
      setErrorMessage('Vui lòng nhập Mật khẩu.');
      return;
    }

    setIsLoading(true);

    try {
      // Gửi yêu cầu đăng nhập lên API ASP.NET Core
      const response = await fetch('/api/auth/login', {
        credentials: 'include',
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          emailOrUsername: loginInput.trim(),
          password: loginPassword,
        }),
      });

      const data = await response.json();

      if (response.ok && data.success) {
        setSuccessMessage('Đăng nhập thành công! Đang chuyển hướng...');
        setTimeout(() => {
          if (onSuccess) onSuccess(data.user);
        }, 800);
      } else {
        setErrorMessage(data.message || 'Tài khoản hoặc mật khẩu không chính xác.');
      }
    } catch (err) {
      console.error('Lỗi kết nối Backend API khi đăng nhập:', err);
      setErrorMessage('Không thể kết nối đến máy chủ xác thực. Vui lòng kiểm tra lại kết nối mạng hoặc thử lại sau!');
    } finally {
      setIsLoading(false);
    }
  };

  // 2. Xử lý Đăng ký
  const handleRegisterSubmit = async (e) => {
    e.preventDefault();
    setErrorMessage('');
    setSuccessMessage('');

    if (!regUsername.trim()) {
      setErrorMessage('Vui lòng nhập Tên người dùng (Username).');
      return;
    }
    if (regUsername.length < 3 || regUsername.length > 20) {
      setErrorMessage('Tên người dùng phải từ 3 đến 20 ký tự.');
      return;
    }
    if (!regEmail.trim() || !regEmail.includes('@')) {
      setErrorMessage('Vui lòng nhập địa chỉ Email hợp lệ.');
      return;
    }
    if (!regPassword || regPassword.length < 6) {
      setErrorMessage('Mật khẩu phải có tối thiểu 6 ký tự.');
      return;
    }
    if (regPassword !== regConfirmPassword) {
      setErrorMessage('Mật khẩu xác nhận không khớp.');
      return;
    }

    setIsLoading(true);

    try {
      // Gửi yêu cầu đăng ký lên API ASP.NET Core
      const response = await fetch('/api/auth/register', {
        credentials: 'include',
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          username: regUsername.trim(),
          email: regEmail.trim(),
          password: regPassword,
          fullName: regFullName.trim() || regUsername.trim(),
        }),
      });

      const data = await response.json();

      if (response.ok && data.success) {
        setSuccessMessage('Đăng ký tài khoản thành công! Tự động đăng nhập...');
        setTimeout(() => {
          if (onSuccess) onSuccess(data.user);
        }, 1000);
      } else {
        setErrorMessage(data.message || 'Đăng ký không thành công. Vui lòng thử lại!');
      }
    } catch (err) {
      console.error('Lỗi kết nối Backend API khi đăng ký:', err);
      setErrorMessage('Không thể kết nối đến máy chủ xác thực. Vui lòng kiểm tra lại kết nối mạng hoặc thử lại sau!');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="auth-page-wrapper">
      <div className="auth-card">
        {/* Nút quay lại trang chủ */}
        <div className="d-flex justify-content-between align-items-center mb-3">
          <button
            type="button"
            className="auth-home-link"
            onClick={onGoHome}
            title="Quay lại danh sách sản phẩm"
          >
            <i className="bi bi-arrow-left"></i> Quay lại cửa hàng
          </button>
          <span className="badge bg-light text-secondary border">PCSTORE AUTH</span>
        </div>

        {/* =========================================================================
            GIAO DIỆN FORM ĐĂNG NHẬP (SIGN IN) - DÀNH CHO KHÁCH HÀNG
        ========================================================================= */}
        {mode === 'login' ? (
          <div>
            <h1 className="auth-title">Đăng nhập</h1>

            {/* Thông báo lỗi hoặc thành công */}
            {errorMessage && (
              <div className="auth-alert auth-alert-error">
                <i className="bi bi-exclamation-triangle-fill flex-shrink-0"></i>
                <div>{errorMessage}</div>
              </div>
            )}
            {successMessage && (
              <div className="auth-alert auth-alert-success">
                <i className="bi bi-check-circle-fill flex-shrink-0"></i>
                <div>{successMessage}</div>
              </div>
            )}

            <form onSubmit={handleLoginSubmit}>
              {/* E-Mail / Username */}
              <div className="auth-form-group">
                <label className="auth-label">
                  E-Mail hoặc Tên đăng nhập<span className="required-star">*</span>
                </label>
                <div className="auth-input-wrap">
                  <input
                    type="text"
                    className="auth-input"
                    placeholder="ví dụ: khachhang hoặc email@gmail.com"
                    value={loginInput}
                    onChange={(e) => setLoginInput(e.target.value)}
                    autoFocus
                  />
                </div>
              </div>

              {/* Password */}
              <div className="auth-form-group">
                <label className="auth-label">
                  Mật khẩu<span className="required-star">*</span>
                </label>
                <div className="auth-input-wrap">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    className="auth-input"
                    placeholder="Nhập mật khẩu của bạn"
                    value={loginPassword}
                    onChange={(e) => setLoginPassword(e.target.value)}
                  />
                  <button
                    type="button"
                    className="auth-toggle-pwd"
                    onClick={() => setShowPassword(!showPassword)}
                    title={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
                  >
                    <i className={`bi ${showPassword ? 'bi-eye-slash' : 'bi-eye'}`}></i>
                  </button>
                </div>
              </div>

              {/* Submit Button */}
              <button
                type="submit"
                className="auth-submit-btn"
                disabled={isLoading}
              >
                {isLoading ? (
                  <>
                    <span className="spinner-border spinner-border-sm" role="status"></span>
                    Đang xử lý...
                  </>
                ) : (
                  'Đăng nhập'
                )}
              </button>

              {/* Forgot Password */}
              <a
                href="#forgot"
                className="auth-forgot-link"
                onClick={(e) => {
                  e.preventDefault();
                  alert('Để đặt lại mật khẩu, vui lòng liên hệ bộ phận hỗ trợ khách hàng qua Hotline 1800 1234 hoặc email: support@pcstore.vn.');
                }}
              >
                Quên mật khẩu?
              </a>

              {/* Dòng chuyển sang Đăng ký */}
              <p className="auth-switch-text mt-3 mb-0">
                Bạn chưa có tài khoản?
                <button
                  type="button"
                  className="auth-switch-link"
                  onClick={() => switchMode('register')}
                >
                  Hãy đăng ký
                </button>
              </p>
            </form>
          </div>
        ) : (
          /* =========================================================================
             GIAO DIỆN FORM ĐĂNG KÝ (SIGN UP) - DÀNH CHO KHÁCH HÀNG MỚI
          ========================================================================= */
          <div>
            <h1 className="auth-title">Tạo tài khoản</h1>

            {/* Thông báo */}
            {errorMessage && (
              <div className="auth-alert auth-alert-error">
                <i className="bi bi-exclamation-triangle-fill flex-shrink-0"></i>
                <div>{errorMessage}</div>
              </div>
            )}
            {successMessage && (
              <div className="auth-alert auth-alert-success">
                <i className="bi bi-check-circle-fill flex-shrink-0"></i>
                <div>{successMessage}</div>
              </div>
            )}

            <form onSubmit={handleRegisterSubmit}>
              {/* Username */}
              <div className="auth-form-group">
                <label className="auth-label">
                  Tên người dùng (Username)<span className="required-star">*</span>
                </label>
                <div className="auth-input-wrap">
                  <input
                    type="text"
                    className="auth-input"
                    placeholder="ví dụ: user123, quangvinh..."
                    value={regUsername}
                    onChange={(e) => setRegUsername(e.target.value)}
                    autoFocus
                  />
                </div>
                <span className="auth-field-hint">4-20 ký tự: chữ cái (a-z), chữ số (0-9) và dấu gạch dưới</span>
              </div>

              {/* Full Name */}
              <div className="auth-form-group">
                <label className="auth-label">Họ và tên</label>
                <div className="auth-input-wrap">
                  <input
                    type="text"
                    className="auth-input"
                    placeholder="ví dụ: Nguyễn Văn A"
                    value={regFullName}
                    onChange={(e) => setRegFullName(e.target.value)}
                  />
                </div>
              </div>

              {/* Email */}
              <div className="auth-form-group">
                <label className="auth-label">
                  Địa chỉ E-Mail<span className="required-star">*</span>
                </label>
                <div className="auth-input-wrap">
                  <input
                    type="email"
                    className="auth-input"
                    placeholder="ví dụ: ban@gmail.com"
                    value={regEmail}
                    onChange={(e) => setRegEmail(e.target.value)}
                  />
                </div>
              </div>

              {/* Password */}
              <div className="auth-form-group">
                <label className="auth-label">
                  Mật khẩu<span className="required-star">*</span>
                </label>
                <div className="auth-input-wrap">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    className="auth-input"
                    placeholder="Tối thiểu 6 ký tự"
                    value={regPassword}
                    onChange={(e) => setRegPassword(e.target.value)}
                  />
                  <button
                    type="button"
                    className="auth-toggle-pwd"
                    onClick={() => setShowPassword(!showPassword)}
                    title={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
                  >
                    <i className={`bi ${showPassword ? 'bi-eye-slash' : 'bi-eye'}`}></i>
                  </button>
                </div>
                <span className="auth-field-hint">Mật khẩu sẽ được mã hóa băm bảo mật SHA256 an toàn</span>
              </div>

              {/* Confirm Password */}
              <div className="auth-form-group">
                <label className="auth-label">
                  Xác nhận mật khẩu<span className="required-star">*</span>
                </label>
                <div className="auth-input-wrap">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    className="auth-input"
                    placeholder="Nhập lại mật khẩu ở trên"
                    value={regConfirmPassword}
                    onChange={(e) => setRegConfirmPassword(e.target.value)}
                  />
                </div>
              </div>

              {/* Submit Button */}
              <button
                type="submit"
                className="auth-submit-btn"
                disabled={isLoading}
              >
                {isLoading ? (
                  <>
                    <span className="spinner-border spinner-border-sm" role="status"></span>
                    Đang tạo tài khoản...
                  </>
                ) : (
                  'Đăng ký tài khoản'
                )}
              </button>

              {/* Dòng chuyển sang Đăng nhập */}
              <p className="auth-switch-text mt-3 mb-0">
                Bạn đã có tài khoản?
                <button
                  type="button"
                  className="auth-switch-link"
                  onClick={() => switchMode('login')}
                >
                  Hãy đăng nhập
                </button>
              </p>
            </form>
          </div>
        )}
      </div>
    </div>
  );
}
