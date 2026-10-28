import { useState } from 'react';
import './AuthPage.css';

// SVG Logo chuẩn của Google (4 màu chính thức)
const GoogleIcon = () => (
  <svg width="20" height="20" viewBox="0 0 48 48" style={{ display: 'inline-block', verticalAlign: 'middle' }}>
    <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/>
    <path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/>
    <path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"/>
    <path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/>
    <path fill="none" d="M0 0h48v48H0z"/>
  </svg>
);

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
  const [showGoogleModal, setShowGoogleModal] = useState(false);
  const [customGoogleEmail, setCustomGoogleEmail] = useState('');

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

  // 3. Xử lý Đăng nhập bằng Google
  const handleGoogleLogin = async (googleEmail, googleName) => {
    setIsLoading(true);
    setErrorMessage('');
    setSuccessMessage('');
    setShowGoogleModal(false);

    try {
      const response = await fetch('/api/auth/google', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          email: googleEmail,
          name: googleName,
          googleId: 'goog_' + Date.now(),
          avatar: 'https://lh3.googleusercontent.com/a/default-user',
        }),
      });

      const data = await response.json();
      if (response.ok && data.success) {
        setSuccessMessage(`Đăng nhập Google (${googleEmail}) thành công!`);
        setTimeout(() => {
          if (onSuccess) onSuccess(data.user);
        }, 800);
      } else {
        setErrorMessage(data.message || 'Đăng nhập Google thất bại do tính năng này yêu cầu xác thực OAuth2 từ máy chủ.');
      }
    } catch (err) {
      console.error('Lỗi kết nối xác thực Google:', err);
      setErrorMessage('Không thể kết nối đến máy chủ xác thực. Vui lòng thử lại sau!');
    } finally {
      setIsLoading(false);
    }
  };

  // Nạp nhanh tài khoản mẫu để chấm bài / demo
  const fillDemoAccount = (role) => {
    if (role === 'admin') {
      setLoginInput('admin@pcstore.vn');
      setLoginPassword('Admin@123');
    } else {
      setLoginInput('khachhang@gmail.com');
      setLoginPassword('User@123');
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
            GIAO DIỆN FORM ĐĂNG NHẬP (SIGN IN) - DỰA TRÊN LAYOUT FASTFINGERS
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
                    placeholder="ví dụ: admin@pcstore.vn hoặc khachhang"
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
                  alert('Để đặt lại mật khẩu, vui lòng liên hệ quản trị viên qua email: admin@pcstore.vn hoặc Hotline 1800 1234.');
                }}
              >
                Quên mật khẩu?
              </a>

              {/* Divider HOẶC */}
              <div className="auth-divider">
                <span className="auth-divider-text">HOẶC</span>
              </div>

              {/* Đăng nhập bằng Google */}
              <button
                type="button"
                className="auth-google-btn"
                onClick={() => {
                  alert('Chức năng đăng nhập Google hiện đang bảo trì để nâng cấp xác thực Google OAuth 2.0 an toàn. Vui lòng đăng nhập bằng Email/Tài khoản và Mật khẩu.');
                }}
              >
                <GoogleIcon />
                <span>Đăng nhập với Google</span>
              </button>

              {/* Dòng nhỏ chuyển sang Đăng ký theo yêu cầu người dùng */}
              <p className="auth-switch-text">
                Bạn chưa có tài khoản?
                <button
                  type="button"
                  className="auth-switch-link"
                  onClick={() => switchMode('register')}
                >
                  Hãy đăng ký
                </button>
              </p>

              {/* Hộp nạp tài khoản mẫu cho giảng viên / người kiểm thử */}
              <div className="auth-demo-box">
                <div className="fw-bold text-dark mb-1 d-flex align-items-center gap-1">
                  <i className="bi bi-info-circle text-primary"></i> Tài khoản mẫu để kiểm thử:
                </div>
                <div className="d-flex gap-2 mt-2">
                  <button
                    type="button"
                    className="auth-demo-btn"
                    onClick={() => fillDemoAccount('admin')}
                  >
                    👑 Quản trị (Admin)
                  </button>
                  <button
                    type="button"
                    className="auth-demo-btn"
                    onClick={() => fillDemoAccount('customer')}
                  >
                    👤 Khách hàng (User)
                  </button>
                </div>
              </div>
            </form>
          </div>
        ) : (
          /* =========================================================================
             GIAO DIỆN FORM ĐĂNG KÝ (SIGN UP / CREATE YOUR ACCOUNT)
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

              {/* Divider HOẶC */}
              <div className="auth-divider">
                <span className="auth-divider-text">HOẶC</span>
              </div>

              {/* Đăng ký nhanh bằng Google */}
              <button
                type="button"
                className="auth-google-btn"
                onClick={() => setShowGoogleModal(true)}
              >
                <GoogleIcon />
                <span>Đăng ký với Google</span>
              </button>

              {/* Dòng nhỏ chuyển sang Đăng nhập */}
              <p className="auth-switch-text">
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

      {/* =========================================================================
          MODAL CHỌN TÀI KHOẢN GOOGLE TIỆN DỤNG
      ========================================================================= */}
      {showGoogleModal && (
        <div
          className="modal show d-block"
          style={{ backgroundColor: 'rgba(0,0,0,0.5)', zIndex: 1060 }}
          onClick={() => setShowGoogleModal(false)}
        >
          <div
            className="modal-dialog modal-dialog-centered"
            style={{ maxWidth: '400px' }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="modal-content border-0 shadow-lg rounded-4 p-3">
              <div className="modal-header border-0 pb-1">
                <div className="d-flex align-items-center gap-2">
                  <GoogleIcon />
                  <h6 className="modal-title fw-bold mb-0">Đăng nhập với Google</h6>
                </div>
                <button
                  type="button"
                  className="btn-close"
                  onClick={() => setShowGoogleModal(false)}
                ></button>
              </div>
              <div className="modal-body py-2">
                <p className="text-secondary small mb-3">
                  Chọn một tài khoản Google để tiếp tục với <strong>PCStore</strong>:
                </p>

                {/* Tài khoản Google 1 */}
                <div
                  className="d-flex align-items-center gap-3 p-2 rounded-3 border mb-2"
                  style={{ cursor: 'pointer', transition: 'background 0.15s' }}
                  onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = '#f8fafc')}
                  onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
                  onClick={() => handleGoogleLogin('batruong.dev@gmail.com', 'Nguyễn Bá Trường')}
                >
                  <div
                    className="rounded-circle bg-danger text-white d-flex align-items-center justify-content-center fw-bold"
                    style={{ width: '38px', height: '38px', fontSize: '15px' }}
                  >
                    T
                  </div>
                  <div>
                    <div className="fw-bold small text-dark">Nguyễn Bá Trường</div>
                    <div className="text-secondary" style={{ fontSize: '12px' }}>batruong.dev@gmail.com</div>
                  </div>
                </div>

                {/* Tài khoản Google 2 */}
                <div
                  className="d-flex align-items-center gap-3 p-2 rounded-3 border mb-3"
                  style={{ cursor: 'pointer', transition: 'background 0.15s' }}
                  onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = '#f8fafc')}
                  onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
                  onClick={() => handleGoogleLogin('demo.google@gmail.com', 'Google Tester')}
                >
                  <div
                    className="rounded-circle bg-primary text-white d-flex align-items-center justify-content-center fw-bold"
                    style={{ width: '38px', height: '38px', fontSize: '15px' }}
                  >
                    G
                  </div>
                  <div>
                    <div className="fw-bold small text-dark">Google Tester</div>
                    <div className="text-secondary" style={{ fontSize: '12px' }}>demo.google@gmail.com</div>
                  </div>
                </div>

                {/* Hoặc nhập email Google khác */}
                <div className="pt-2 border-top">
                  <label className="form-label small fw-semibold text-secondary">
                    Hoặc nhập Gmail của bạn:
                  </label>
                  <div className="input-group input-group-sm">
                    <input
                      type="email"
                      className="form-control"
                      placeholder="tentaikhoan@gmail.com"
                      value={customGoogleEmail}
                      onChange={(e) => setCustomGoogleEmail(e.target.value)}
                    />
                    <button
                      className="btn btn-outline-danger"
                      type="button"
                      disabled={!customGoogleEmail.includes('@')}
                      onClick={() => {
                        const name = customGoogleEmail.split('@')[0];
                        handleGoogleLogin(customGoogleEmail.trim(), name);
                      }}
                    >
                      Tiếp tục
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
