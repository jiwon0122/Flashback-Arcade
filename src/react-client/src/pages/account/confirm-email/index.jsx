import { Link, useSearchParams } from "react-router-dom";
import ENDPOINTS, { API_BASE_URL } from "../../../configs/api-endpoints";
import { useEffect, useState } from "react";
import { Button } from "react-bootstrap";
import { toast } from "react-toastify";

export default function ConfirmEmail() {
    const [searchParams, setSearchParams] = useSearchParams();
    const token = searchParams.get("token");
    const email = searchParams.get("email");

    const [msg, setMsg] = useState("");

    useEffect(() => {
        if (token !== undefined && email !== undefined) {
            fetch(ENDPOINTS.POST_API_ACCOUNT_CONFIRM_EMAIL, {
                method: 'POST',
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    token: token,
                    email: email
                })
            })
                .then(response => {
                    console.log(response.status)
                    return response.json();
                })
                .then(responseFromServer => {
                    console.log(responseFromServer.msg);
                    toast(responseFromServer.msg);
                    setMsg(responseFromServer.msg);
                })
                .catch(error => console.log(error));
        }
        else {
            console.log("All undefined");
        }

    }, [])

    return (
        <>
            { msg &&
                (
                    <div>
                        {msg}
                        <Button as={Link} to="/account/signin">로그인 페이지로 이동</Button>
                    </div>
                )
            }
        </>
    )
}
